using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO
//1.Drop power and bonus points 
//2.Start particle when enemy is killed
//3.Free mode: (I'm not sure)
//Some random movements
//When enemy is going to out of bounds it got destroy
//4.Make many types of enemy 
//It will be have diffrent models, bullet pattern 
//Diffrent types
//Weak  (Free mode?, no shots)
//Strong (Not free)

public class Enemy : MovableObject
{
    [Header("Weapon")]
    public float            shotFrequency = 0.05f;
    public bool             weaponEnabled = false;
    public Bullet           bulletPrefab;
    public bool             canUseWeapon = true;
    public bool             ignoreHits = false;

    [Header("Enemy")]
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected bool weakType = false; 
    [SerializeField] protected bool onLastPointWillDestroy = false;
    [SerializeField] protected bool _isDead = false;
    [SerializeField] protected int _lifes = 1;
    [SerializeField] protected float _maxHealth = 0.0f;
    [SerializeField] protected float _health = 0.0f;
    [SerializeField] protected float _reduceHealth = 1.5f;

    [Header("Misc")] 
    [SerializeField] protected PlayerController _playerController;
    [SerializeField] protected ParticleSystem   _deathParticle;
    [SerializeField] protected Coroutine _shotCoroutine;

    protected void Start()
    {
        _rigidBody2D = GetComponent<Rigidbody2D>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _health = _maxHealth;

        EnemyStart();
    }

    public override void OnReachedPointEvent(EnemyScriptedBehavior enemyScriptedBehavior)
    {
        switch(enemyScriptedBehavior)
        {
            case EnemyScriptedBehavior.StartShot:
                StartShot();
                break;
            case EnemyScriptedBehavior.StopShot:
                StopShot();
                break;
        }
    }

    public override void OnReachedFirstPoint()
    {
        ignoreHits = false;
    }

    public override void OnReachedLastPoint()
    {
        if (onLastPointWillDestroy)
        {
            Kill(false, false);
            return;
        }

        ignoreHits = true;
    }

    public void StartShot()
    {
        OnStartShot();
        weaponEnabled = true;
        _shotCoroutine = StartCoroutine(Shot());
    }

    public void StopShot()
    {
        OnStopShot();
        weaponEnabled = false;

        if(_shotCoroutine != null)
            StopCoroutine(_shotCoroutine);
    }

    private IEnumerator Shot() 
    {
        while (GlobalSettings.gameActiveBool && (weaponEnabled && canUseWeapon && !_isDead))
        {
            OnShot();
            Vector2 final_pos = new Vector2(rigidBody2D.position.x, rigidBody2D.position.y - boxCollider2D.size.y);
            Instantiate(bulletPrefab, final_pos, Quaternion.identity);
            yield return new WaitForSeconds(shotFrequency);
        }
    }

    public void Kill(bool givePlayerScore, bool playParticle = true)
    {
        _lifes  = 0;
        _health = 0;
        _isDead = true;

        OnDieCompletely();

        if(givePlayerScore)
            if(_playerController)
                _playerController.Score += 1000;

        //TODO: Add sounds
        if (_deathParticle && playParticle)
        {
            _pathSystem.DetachObject(this);
            _spriteRenderer.enabled = false;
            _boxCollider2D.enabled = false;
            ParticleSystemRenderer pr = _deathParticle.GetComponent<ParticleSystemRenderer>();            
            pr.material.color = _spriteRenderer.color;
            _deathParticle.Play();
            Destroy(gameObject, _deathParticle.main.duration);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Damage()
    {
        if (ignoreHits)
            return;

        OnDamage();
        if (_health <= 0.0f)
        {
            _health = _maxHealth;
            _lifes--;

            OnDie();

            if (_lifes == 0)
                Kill(true);
        }
        else
        {
            _health -= _reduceHealth;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Component[] components = collision.gameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            switch (component)
            {
                case PlayerController p:
                    if (weakType)
                        Kill(false);
                    break;
                case Bullet b:
                    Damage();
                    break;
            }
        }
    }

    protected virtual void EnemyStart()
    {
        _playerController = (PlayerController)FindObjectOfType(typeof(PlayerController));
        if(_playerController)
            _playerController.maxScore += 1000;
    }

    public virtual void OnShot()
    {

    }

    public virtual void OnStopShot()
    {

    }

    public virtual void OnStartShot()
    {

    }

    public virtual void OnDamage()
    {

    }

    public virtual void OnDie()
    {

    }

    public virtual void OnDieCompletely()
    {
        
    }
}
