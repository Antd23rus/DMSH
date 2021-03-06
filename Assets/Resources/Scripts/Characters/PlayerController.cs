using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MovableObject
{
    #region Public Values
    public int Life
    {
        get => _player_life;
        set
        {
            if (value >= PLAYER_MIN_LIFE)
                _player_life = value;

            UpdateHUD();
        }
    }
    public int Boost
    {
        get => _player_boost;
        set
        {
            if (value >= PLAYER_MIN_LIFE)
                _player_boost = value;

            UpdateHUD();
        }
    }

    public int Score
    {
        get => _player_score;
        set
        {
            _player_score = value;

            UpdateHUD();
        }
    }

    public bool CheatGod
    {
        get => _cheatGod;
        set
        {
            _cheatGod = value;
        }
    }

    public bool CheatInfBoost
    {
        get => _cheatInfiniteBoost;
        set
        {
            _cheatInfiniteBoost = value;
        }
    }

    public bool DebugGUI
    {
        get => _debugGUI;
        set
        {
            _debugGUI = value;
        }
    }
    #endregion

    #region Constants
    public const int PLAYER_MAX_LIFE = 3;
    public const int PLAYER_MAX_BOOST = 5;
    public const int PLAYER_MIN_LIFE = -1;
    public const int UI_ZEROS_SCORE_TEXT = 8;
    #endregion

    [Header("Global")]
    public SpriteRenderer   spriteRenderer = null;
    public Camera           gameCamera = null;
    public LogHandler       logHandler = null;
    public GameObject       respawnPoint = null;
    public PlayerInput      playerInput = null;
    public ScreenHandler    screenHandler = null;
    public int              maxScore = 0;

    [SerializeField] private Vector2 _move;
    [SerializeField] private bool    _isDead = false;
    [SerializeField] private CameraDeathAnimation _cameraDeathAnimation = null;

    private Coroutine _showChapterNameCoroutine = null;
    private Coroutine _slowMotionCoroutine = null;
    private Coroutine _deathAwakeCoroutine = null;
    private Coroutine _shotCoroutine = null;

    [Header("Stage")]
    [SerializeField] private StageSystem _stageSystem = null;

    [Header("Statistics")]
    [SerializeField] private int _player_score = 0;
    [SerializeField] private int _player_life = PLAYER_MAX_LIFE;
    [SerializeField] private int _player_boost = PLAYER_MAX_BOOST;
    
    [Header("Boost")]
    [SerializeField] private float _boost_speed = 0.05f;
    [SerializeField] private float _saved_time_scale = 0.0f;

    [Header("Cheats & Debug")]
    [SerializeField] private bool     _debugGUI = false;
    [SerializeField] private GUIStyle _cheatGUIStyle = null;
    [SerializeField] private bool     _cheatGod = false;
    [SerializeField] private bool     _cheatInfiniteBoost = false;

    [Header("UI")]
    [SerializeField] private Text     _uiScoreText = null;
    [SerializeField] private Text     _uiBoostGainText = null;
    [SerializeField] private Text     _uiBoostText = null;
    [SerializeField] private Text     _uiLifeText = null;
    [SerializeField] private Text     _uiFpsCounterText = null;
    [SerializeField] private Text     _uiChapterName = null;
    [SerializeField] private Image    _uiSomeImage = null; //Image on the screen corner

    [Header("UI Screens")]
    [SerializeField] private GameObject _uiPauseScreen = null;
    [SerializeField] private GameObject _uiDeathScreen = null;
    [SerializeField] private Text       _uiCurrentScoreText = null; //Only for death screen
    [SerializeField] private Text       _uiMaxScoreText = null;

    [Header("Weapon")]
    public bool     weaponEnabled = false;
    public Bullet   bulletPrefab = null;
    [SerializeField] private int    _weaponType = 0;
    [SerializeField] private float  _weaponBoostGain = 0.0f;
    [SerializeField] private GameObject _shotPoint = null;
    [SerializeField] private float  _shotFrequency = 0.07f;

    [Header("Resizable")]
    [SerializeField] private GameObject[]   _wallsList = new GameObject[4];
    [SerializeField] private GameObject     _background = null;

    [Header("Sounds")]
    [SerializeField] private AudioSource audioSourceWeapon = null;
    [SerializeField] private AudioSource audioSourceDeath = null;
    [SerializeField] private AudioSource audioSourceMusic = null;

    [Header("Particles")]
    [SerializeField] protected ParticleSystem _deathParticle = null;


    protected void Start()
    {
        //Get all components
        gameCamera          = GetComponentInParent(typeof(Camera)) as Camera;
        spriteRenderer      = GetComponent<SpriteRenderer>();
        rigidBody2D         = GetComponent<Rigidbody2D>();
        boxCollider2D       = GetComponent<BoxCollider2D>();
        playerInput         = GetComponent<PlayerInput>();
        logHandler          = GetComponent<LogHandler>();
        _stageSystem        = FindObjectOfType<StageSystem>();
        screenHandler       = gameObject.AddComponent<ScreenHandler>();
        screenHandler.onScreenResolutionChange.Add(OnResolutionScreenChange);
        _cameraDeathAnimation = gameObject.AddComponent<CameraDeathAnimation>();
        _cameraDeathAnimation.animCamera = gameCamera;
        _cameraDeathAnimation.target = gameObject;

        //First initialize
        UpdateHUD();
        GenerateInvisibleWalls();
        UpdateSettings();
        _uiBoostGainText.text = "100%";

        //Set style for cheat gui
        _cheatGUIStyle.fontSize = 13;
        _cheatGUIStyle.normal.textColor = new Color(255, 0, 0);

        //Don't show cursor when we are create the player 
        Cursor.visible = false;

        //Set respawnPoint position
        gameObject.transform.position = respawnPoint.transform.position;

        //Set timer callback for stage system
        _stageSystem.onTimerStart.Add(ShowChapterName);
        _stageSystem.onTimerEnd.Add(RemoveChapterName);
    }

    public void ShowChapterName()
    {
        _showChapterNameCoroutine = StartCoroutine(BasicAnimationsPack.SmoothAwakeText(_uiChapterName, 255, 15));
        _uiChapterName.text = $"Chapter {_stageSystem.CurrentStageIndex + 1} {_stageSystem.currentStage.name}";
    }

    public void RemoveChapterName()
    {
        if(_showChapterNameCoroutine != null)
            StopCoroutine(_showChapterNameCoroutine);
        _showChapterNameCoroutine = StartCoroutine(BasicAnimationsPack.SmoothFadeText(_uiChapterName, 15));
    }

    private void GenerateInvisibleWalls()
    {
        for (int i = 0; i <= 3; i++)
        {
            _wallsList[i] = new GameObject($"GeneratedInvisibleWall{i}");
            var local_boxCollider2D = _wallsList[i].AddComponent<BoxCollider2D>();
            local_boxCollider2D.size = gameCamera.ViewportToWorldPoint(i <= 1 ? new Vector2(1, 0) : new Vector2(0, 1)) * 2;
            local_boxCollider2D.size += i <= 1 ? new Vector2(0.0f, 0.1f) : new Vector2(0.1f, 0.0f);
            _wallsList[i].layer = 8; // wtf
        }
    }

    protected void FixedUpdate()
    {
        _rigidBody2D.MovePosition(_rigidBody2D.position + ((_move * _speed) * Time.fixedDeltaTime * GlobalSettings.gameActive));
    }

    protected void Update()
    {        
        _uiFpsCounterText.text = $"FPS:{(int)(1f / Time.unscaledDeltaTime)}";
    }

    private void UpdateInvisibleWallsPosition(Vector3 ViewportToWorldPointX, Vector3 ViewportToWorldPointY, Vector3 viewportToWorldPointXWithImage)
    {        
        _wallsList[0].transform.position = ViewportToWorldPointY;
        _wallsList[1].transform.position = -ViewportToWorldPointY;
        _wallsList[2].transform.position = viewportToWorldPointXWithImage;
        _wallsList[3].transform.position = -ViewportToWorldPointX;

        //FIX ME: Sometimes player can bypass invisible walls when this function is called
        //        It's collision bug but we need to avoid this

        //It's very very bad fix for this problem
        if (gameObject.transform.position.x >  _wallsList[2].transform.position.x  || gameObject.transform.position.x < _wallsList[3].transform.position.x ||
            gameObject.transform.position.y > _wallsList[0].transform.position.y || gameObject.transform.position.y <  _wallsList[1].transform.position.y)
                gameObject.transform.position = respawnPoint.transform.position;
    }

    private void OnResolutionScreenChange()
    {
        //Get viewport world points
        Vector3 viewportToWorldPointX = new Vector2(gameCamera.ViewportToWorldPoint(new Vector2(1, 0)).x, 0);
        Vector3 viewportToWorldPointY = new Vector2(0, gameCamera.ViewportToWorldPoint(new Vector2(0, 1)).y);
        Vector3 viewportToWorldPointXWithImage = viewportToWorldPointX - new Vector3(viewportToWorldPointX.x * _uiSomeImage.rectTransform.sizeDelta.x * 20.0f, 0, 0);
        float aspectRatioWithImage = (Vector3.Distance(-viewportToWorldPointX, viewportToWorldPointXWithImage) / Vector3.Distance(viewportToWorldPointY, -viewportToWorldPointY)) + _uiSomeImage.rectTransform.sizeDelta.x * 2;
        UpdateInvisibleWallsPosition(viewportToWorldPointX, viewportToWorldPointY, viewportToWorldPointXWithImage);

        //I guess it's not correct way to implement background restretch
        if (_background)
        {
            _background.transform.localScale = new Vector3(aspectRatioWithImage, Vector3.Distance(viewportToWorldPointY, viewportToWorldPointY), 1);
            _background.transform.position = new Vector3(gameCamera.transform.position.x + -viewportToWorldPointX.x * _uiSomeImage.rectTransform.sizeDelta.x * 9.2f, gameCamera.transform.position.y, 5);
        }

        //Set screen middle position for respawn point
        respawnPoint.transform.position = new Vector2((-viewportToWorldPointX.x * _uiSomeImage.rectTransform.sizeDelta.x) / 1000, -viewportToWorldPointY.y / 1.2f);

        UpdateInvisibleWallsPosition(viewportToWorldPointX, viewportToWorldPointY, viewportToWorldPointXWithImage);
    }

    private IEnumerator Shot()
    {
        while (weaponEnabled)
        {
            Vector3 final_pos = _shotPoint != null ? _shotPoint.transform.position : new Vector3(rigidBody2D.position.x, rigidBody2D.position.y + boxCollider2D.size.y, 0);
            Instantiate(bulletPrefab, final_pos, Quaternion.identity);

            audioSourceWeapon.Play();

            yield return new WaitForSeconds(_shotFrequency);
        }
    }

    private IEnumerator DoSlowMotion(bool isBoost = true)
    {
        while (Time.timeScale < 1.0f)
        {
            Time.fixedDeltaTime = 0.02F * Time.timeScale;
            Time.timeScale += GlobalSettings.gameActive * _boost_speed;

            foreach (AudioSource s in FindObjectsOfType<AudioSource>())
                if(s.gameObject.tag != "NotGenericSound")
                    s.pitch = Time.timeScale;

            if(isBoost)
                _uiBoostGainText.text = $"{(int)(Time.timeScale * 100)}%";

            yield return new WaitForSeconds(.1f);
        }

        _uiBoostGainText.text = "100%";

        Time.timeScale = 1.0f;
    }

    public void UseBoost()
    {
        if ((Boost <= 0 && !_cheatInfiniteBoost) || Time.timeScale < 1.0f)
            return;

        foreach (Bullet bullet in FindObjectsOfType<Bullet>())
            if (bullet.isEnemyBullet &&
                bullet.collisionDestroyBullet &&
                bullet.pathSystem == null)
                    bullet.SqueezeAndDestroy();

        Boost--;
        Time.timeScale = 0.05f;
        _slowMotionCoroutine = StartCoroutine(DoSlowMotion());
    }

    private void OnUseBoost(InputValue input)
    {
        if (GlobalSettings.gameActiveBool)
            UseBoost();
    }

    private void OnShot(InputValue input)
    {
        if (GlobalSettings.gameActiveBool)
        {
            weaponEnabled = input.isPressed;
            _shotCoroutine = StartCoroutine(Shot());
        }
    }

    private void OnMoveH(InputValue input)
    {
        _move.x = input.Get<Vector2>().x;
    }

    private void OnMoveV(InputValue input)
    {
        _move.y = input.Get<Vector2>().y;
    }

    private void OnPause(InputValue input)
    {
        ShowPauseScreen();
    }

    public void ShowDeathScreen()
    {
        Cursor.visible = true;

        //Disable all sounds in scene
        foreach (AudioSource s in FindObjectsOfType<AudioSource>())
            if (s.gameObject.tag != "NotGenericSound")
                s.Stop();

        //TODO: Change track 
        audioSourceMusic.Stop();

        //Show death screen
        _uiDeathScreen.SetActive(!_uiDeathScreen.activeSelf);

        //Stop game world
        GlobalSettings.gameActive = 0;
        Time.timeScale = 1.0f;

        //Show some results
        _uiCurrentScoreText.text += GetNumberWithZeros(Score);
        _uiMaxScoreText.text += GetNumberWithZeros(maxScore);
    }

    public void ShowPauseScreen()
    {
        //Save the last time scale state
        if (_uiPauseScreen.activeSelf == false)
            _saved_time_scale = Time.timeScale;

        //Enable or disable pause menu
        _uiPauseScreen.SetActive(!_uiPauseScreen.activeSelf);
        Cursor.visible = _uiPauseScreen.activeSelf;
        GlobalSettings.gameActive = System.Convert.ToInt32(!_uiPauseScreen.activeSelf);
        Time.timeScale = _uiPauseScreen.activeSelf == false ? _saved_time_scale : 1.0f;

        //TODO: Change track 
        if (_uiPauseScreen.activeSelf == false)
        {
            //Enable boost if we are exit from pause menu and
            //if we hasnt enable boost in game we are skip the loop because loop work if Time.timeScale < 1.0f
            _slowMotionCoroutine = StartCoroutine(DoSlowMotion());
            //Enable death animation
            if(spriteRenderer.color.a < 0.9f)
                _deathAwakeCoroutine = StartCoroutine(SmoothAwake(spriteRenderer));

            audioSourceMusic.Play();
        }
        else
        {
            weaponEnabled = false;

            if (_deathAwakeCoroutine != null)
                StopCoroutine(_deathAwakeCoroutine);

            if (_shotCoroutine != null)
                StopCoroutine(_shotCoroutine);

            //If we have enabled boost
            if (_slowMotionCoroutine != null)
                StopCoroutine(_slowMotionCoroutine);

            audioSourceMusic.Pause();
        }

        playerInput.currentActionMap.Disable();
        playerInput.SwitchCurrentActionMap(_uiPauseScreen.activeSelf == false ? "Player" : "Pause");
        playerInput.currentActionMap.Enable();

        UpdateSettings();
    }

    private void OnGUI()
    {
        if (_cheatGod)            
            GUI.Label(new Rect(0, 60, 500, 500), "[God]", _cheatGUIStyle);
        
        if (_cheatInfiniteBoost)
            GUI.Label(new Rect(0, 80, 500, 500), "[Infinity boost]", _cheatGUIStyle);
        
        if (_debugGUI)
        {            
            GUI.Label(new Rect(100, 80, 500, 500), "DeltaTime: " + Time.deltaTime);
            GUI.Label(new Rect(100, 120, 500, 500), $"Position: {rigidBody2D.position}");
            GUI.Label(new Rect(100, 140, 500, 500), $"Velocity: {rigidBody2D.velocity}");
            GUI.Label(new Rect(100, 200, 500, 500), $"WeaponEnabled: {weaponEnabled}");
            GUI.Label(new Rect(100, 280, 500, 500), $"Time scale: {Time.timeScale}");
            GUI.Label(new Rect(100, 300, 500, 500), $"Saved time scale: {_saved_time_scale}");
            GUI.Label(new Rect(100, 320, 500, 500), $"gameActive: {GlobalSettings.gameActive}");
            GUI.Label(new Rect(100, 340, 500, 500), $"_weaponBoostGain: {_weaponBoostGain}");
            GUI.Label(new Rect(100, 360, 500, 500), $"_weaponType: {_weaponType}");
        }      
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!audioSourceDeath.isPlaying) 
        {
            Component[] components = collision.gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                switch (component)
                {
                    case Enemy b:
                        Damage();
                        break;
                    case Bullet b:
                        if (b.isEnemyBullet)
                            Damage();                        
                        break;
                }
            }
        }
    }
    private IEnumerator SmoothAwake(SpriteRenderer sprite)
    {
        float speed = 8.0f;
        sprite.color = sprite.color.a >= 0.9f ? new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.0f) : sprite.color;
        while (sprite.color.a <= 1.0f)
        {
            sprite.color = Color.Lerp(sprite.color, new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1.0f), Time.deltaTime * speed);
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void Kill()
    {
        _cameraDeathAnimation.Play();

        if (_slowMotionCoroutine != null)
            StopCoroutine(_slowMotionCoroutine);

        weaponEnabled = false;
        if (_shotCoroutine != null)
            StopCoroutine(_shotCoroutine);

        Life = PLAYER_MIN_LIFE;

        _isDead = true;
        spriteRenderer.enabled = false;
        boxCollider2D.enabled = false;
        rigidBody2D.isKinematic = true;
        playerInput.enabled = false;

        //Show death screen 
        ShowDeathScreen();
    }

    public void Damage()
    {
        //Play player death sound 
        audioSourceDeath.Play();
       
        if (_deathParticle)
        {
            ParticleSystemRenderer pr = _deathParticle.GetComponent<ParticleSystemRenderer>();
            pr.material.color = Color.red;
            _deathParticle.transform.position = rigidBody2D.transform.position;
            _deathParticle.Play();
        }

        //Don't continue if we are dead or in god mode
        if (/*_debug_god || */_isDead == true)
            return;

        //Subtract one life point 
        if(!_cheatGod)
            Life -= 1;


        //If life equal MIN_LIFE we are disable player components for move and body 
        //If we are still alive we are teleport player to spawn point
        if (Life == PLAYER_MIN_LIFE)
            Kill();
        else
        {
            _deathAwakeCoroutine = StartCoroutine(SmoothAwake(spriteRenderer));

            //Make everything slow
            Time.timeScale = 0.2f;
            _slowMotionCoroutine = StartCoroutine(DoSlowMotion(false));
            //Destroy all bullet cuz we are can teleport player into bullet 
            foreach (Bullet bullet in FindObjectsOfType<Bullet>())
                if (bullet.isEnemyBullet &&
                    bullet.collisionDestroyBullet &&
                    bullet.pathSystem == null)
                        bullet.SqueezeAndDestroy();

            //Set spawn point position to player 
            gameObject.transform.position = respawnPoint.transform.position;
        }
    }
    
    //Basic string tool to fill string with number also zeros 
    public string GetNumberWithZeros(int num)
    {
        //Initialize empty string
        string text = string.Empty;
        //Fill string by UI_ZEROS_SCORE_TEXT count subtract number length 
        for (int i = 0; i <= UI_ZEROS_SCORE_TEXT - num.ToString().Length; i++)
            text += "0";
        //Add number
        text += num.ToString();
        return text;
    }

    public void UpdateHUD()
    {
        _uiLifeText.text  = Life.ToString();
        _uiScoreText.text = GetNumberWithZeros(Score);
        _uiBoostText.text = Boost.ToString();
    }

    //Thats update all attached to player game things 
    //Like a volume of music, playing state, or other player sound sources
    //Or something related to gameplay
    public void UpdateSettings()
    {
        audioSourceMusic.enabled = GlobalSettings.musicPlay;
    }

    public void AddWeaponBoost(float gain)
    {
        _weaponBoostGain += gain;
        if(_weaponBoostGain >= 100.0f)
        {
            //TODO: Activate needed weapon type
            _weaponBoostGain = 0.0f;
            _weaponType += 1;
        }

    }
}

