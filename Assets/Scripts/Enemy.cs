﻿using UnityEngine;
using System.Collections;

[RequireComponent (typeof (NavMeshAgent))]
public class Enemy : LivingEntity {

    public ParticleSystem deathEffect;

    public enum State { Idle, Chasing, Attacking };

    public float refreshRate = .25f;
    public float atkDistThreshhold = .5f;
    public float timeBetweenAttacks = 1;
    public float attackSpeed = 3;
    public float damage = 1;

    Material skinMaterial;
    Color originalColor;
    
    private State currentState;

    private NavMeshAgent pathfinder;
    private Transform target;
    private LivingEntity targetEntity;
    
    private float sqrAttackDistanceThreshhold;
    private float nextAttackTime;
    private float myColRadius;
    private float targetColRadius;

    bool hasTarget;

    void Awake() {
        pathfinder = GetComponent<NavMeshAgent>();

        //set state
        if (GameObject.FindGameObjectWithTag("Player") && (target = GameObject.FindGameObjectWithTag("Player").transform)) {
            hasTarget = true;
            targetEntity = target.GetComponent<LivingEntity>();
            //initialize variables
            myColRadius = GetComponent<CapsuleCollider>().radius;
            targetColRadius = target.GetComponent<CapsuleCollider>().radius;
            // math optimization
            sqrAttackDistanceThreshhold = Mathf.Pow(atkDistThreshhold + myColRadius + targetColRadius, 2);
        }
    }

    protected override void Start () {
        base.Start();

        if (hasTarget) {
            currentState = State.Chasing;
            targetEntity.OnDeath += OnTargetDeath;

            StartCoroutine(UpdatePath());
        }
    }

    public void SetCharacteristics(float moveSpeed, int hitsToKillPlayer, float enemyHealth, Color skinColor) {
        pathfinder.speed = moveSpeed;

        if(hasTarget) {
            damage = Mathf.Ceil(targetEntity.startingHealth / hitsToKillPlayer);
        }

        startingHealth = enemyHealth;

        skinMaterial = GetComponent<Renderer>().material;
        skinMaterial.color = skinColor;
        originalColor = skinMaterial.color;
    }

    void OnTargetDeath() {
        hasTarget = false;
        currentState = State.Idle;
    }
	
	void Update () {
        if (hasTarget && Time.time > nextAttackTime) {
            float sqrDistanceToTarget = (target.position - transform.position).sqrMagnitude;
            if (sqrDistanceToTarget < sqrAttackDistanceThreshhold) {
                nextAttackTime = Time.time + timeBetweenAttacks;
                StartCoroutine(Attack());
            } 
        }

	}

    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection) {

        if(damage >= health) {
            Destroy((GameObject)Instantiate(deathEffect.gameObject, hitPoint, 
                Quaternion.FromToRotation(Vector3.forward, hitDirection)), deathEffect.startLifetime);
        }
        base.TakeHit(damage, hitPoint, hitDirection);
    }

    IEnumerator Attack() {
        currentState = State.Attacking;
        pathfinder.enabled = false;

        Vector3 originalPostion = transform.position;
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - dirToTarget * myColRadius;

        float percent = 0;

        skinMaterial.color = Color.red;
        bool hasAppliedDamage = false;

        while (percent <= 1) {

            if(percent >= .5f && !hasAppliedDamage) {
                hasAppliedDamage = true;
                targetEntity.TakeDamage(damage);
            }

            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            transform.position = Vector3.Lerp(originalPostion, attackPosition, interpolation);
            yield return null;
        }

        skinMaterial.color = originalColor;
        currentState = State.Chasing;
        pathfinder.enabled = true;
    }
    
    IEnumerator UpdatePath() {
        if(dead) yield break;
        while (hasTarget) {
            if (currentState == State.Chasing) {
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - dirToTarget * 
                    (myColRadius + targetColRadius + atkDistThreshhold/2);
                if(!dead) pathfinder.SetDestination(targetPosition); 
            }
            yield return new WaitForSeconds(refreshRate); 
        }
    }
}
