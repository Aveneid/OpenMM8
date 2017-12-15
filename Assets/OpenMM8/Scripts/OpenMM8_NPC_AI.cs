﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(OpenMM8_NPC_Stats))]
[RequireComponent(typeof(Animator))]

public abstract class OpenMM8_NPC_AI : MonoBehaviour
{
    public enum NPCState { Walking, Idle, MeleeAttacking, RangedAttacking, Stunned, Dying, Dead, Fidgeting }

    //-------------------------------------------------------------------------
    // Variables
    //-------------------------------------------------------------------------

    // Public - Editor accessible
    public float m_StoppingDistance = 0.5f;

    public float m_MinWanderIdleTime = 1.0f;
    public float m_MaxWanderIdleTime = 2.0f;
    public float m_WanderRadius = 15.0f;

    public bool m_DrawWaypoint = true;

    public float m_AgroRange; // Agro on Y axis is not taken into account
    public float m_MeleeRange;

    public Vector3 m_SpawnPosition;

    // Private
    protected GameObject m_Player;

    protected Animator m_Animator;
    protected NavMeshAgent m_NavMeshAgent;

    protected OpenMM8_NPC_Stats m_Stats;
    
    protected Vector3 m_CurrentDestination;

    protected float m_RemainingWanderIdleTime = 2.0f;

    protected GameObject m_CurrentWaypoint;

    protected NPCState m_State = NPCState.Idle;

    protected List<GameObject> m_EnemiesInMeleeRange = new List<GameObject>();
    protected List<GameObject> m_EnemiesInAgroRange = new List<GameObject>();

    protected GameObject m_Target;

    // State members
    protected string m_Faction;
    protected int m_FleeHealthPercantage;

    protected bool m_IsPlayerInMeleeRange = false;

    protected bool m_IsWalking = false;

    //-------------------------------------------------------------------------
    // Unity Overrides
    //-------------------------------------------------------------------------

    // Use this for initialization
    public void OnStart ()
    {
        m_Player = GameObject.FindWithTag("Player");
        if (m_Player == null)
        {
            Debug.LogError("Could not find \"Player\" in scene !");
        }

        m_SpawnPosition = transform.position;
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();

        // Create debug waypoint
        m_CurrentWaypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        m_CurrentWaypoint.gameObject.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        m_CurrentWaypoint.GetComponent<Renderer>().material.color = Color.red;
        m_CurrentWaypoint.name = this.gameObject.name + " Waypoint";
        m_CurrentWaypoint.GetComponent<SphereCollider>().enabled = false;
    }

    /**** If NPC is a Guard ****/
    // 1) If it is attacking, do nothing (Waiting for AttackEnded frame event)
    // 2) If it is moving, do nothing (May be interrupted if enemy enters its melee range)
    // 3) If it has hostile unit(s) in range, move to its closest one
    // 4) Else If this unit can Patrol, move to its point within patrol area
    // 5) Else do nothing (Idle)
    // ----- [Event] OnAttackEnded - after attack ends, it will check if it is within melee range of any hostile unit,
    //                           if it is, then it will attack it again, if it is not, it will choose some strafe
    //                           location - e.g. Shoot - Move - Shoot - Move, etc.
    // ------ [Event] If enemy enters its attack range, it will attack immediately
    // ------ [Event] OnDamaged - If it was attacked by a unit which was previously friendly, change this unit to Hostile
    //                            and query all nearby Guards / Villagers of the same affiliation to be hostile towards
    //                            that unit too

    /**** If NPC is Enemy ****/
    // 1) If it is attacking, do nothing (Waiting for AttackEnded frame event)
    // 2) If it is moving, do nothing (May be interrupted if enemy enters its melee range)
    // ----- [Event] OnAttackEnded - after attack ends, it will check if it is within melee range of any hostile unit,
    //                           if it is, then it will attack it again, if it is not, it will choose some strafe
    //                           location - e.g. Shoot - Move - Shoot - Move, etc.
    // ------ [Event] If enemy enters its attack range, it will attack immediately

    //-------------------------------------------------------------------------
    // Methods
    //-------------------------------------------------------------------------

    public bool IsOnMove()
    {
        if (!m_NavMeshAgent.pathPending)
        {
            if (m_NavMeshAgent.remainingDistance <= m_NavMeshAgent.stoppingDistance)
            {
                m_NavMeshAgent.SetDestination(transform.position);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                return false;
            }
        }

        return true;
    }

    public void WanderWithinSpawnArea()
    {
        m_CurrentDestination = m_SpawnPosition + new Vector3(Random.Range((int) - m_WanderRadius * 0.5f - 2, (int)m_WanderRadius * 0.5f + 2), 0, Random.Range((int) - m_WanderRadius * 0.5f - 2, (int)m_WanderRadius * 0.5f + 2));
        m_NavMeshAgent.ResetPath();

        m_NavMeshAgent.SetDestination(m_CurrentDestination);

        m_CurrentWaypoint.transform.position = m_CurrentDestination;

        Vector3 direction = (m_CurrentDestination - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
        //transform.rotation = Quaternion.Slerp(transform.rotation, qDir, Time.deltaTime * rotSpeed);
    }

    public void WanderAwayFromEnemy(GameObject enemy)
    {
        // TODO
    }
}

//============================================================
// EDITOR
//============================================================

#if UNITY_EDITOR
[CustomEditor(typeof(OpenMM8_NPC_AI))]
public class OpenMM8_NPC_AI_Editor : Editor
{
    OpenMM8_NPC_AI m_TargetObject;

    public void OnSceneGUI()
    {
        m_TargetObject = this.target as OpenMM8_NPC_AI;

        Handles.color = new Color(0, 1.0f, 0, 0.1f);
        if (EditorApplication.isPlaying)
        {
            Handles.DrawSolidDisc(m_TargetObject.m_SpawnPosition, Vector3.up, m_TargetObject.m_WanderRadius);
        }
        else
        {
            Handles.DrawSolidDisc(m_TargetObject.transform.position, Vector3.up, m_TargetObject.m_WanderRadius);
        }

        MeleeRangeTrigger mrt = m_TargetObject.GetComponentInChildren<MeleeRangeTrigger>();
        if (mrt != null)
        {
            Handles.color = new Color(1.0f, 0.0f, 0, 0.15f);
            Handles.DrawSolidDisc(m_TargetObject.transform.position, Vector3.up, mrt.m_MeleeRangeRadius);
        }

        AgroRangeTrigger art = m_TargetObject.GetComponentInChildren<AgroRangeTrigger>();
        if (art != null)
        {
            Handles.color = new Color(1.0f, 1.0f, 0, 0.15f);
            Handles.DrawSolidDisc(m_TargetObject.transform.position, Vector3.up, art.m_AgroRangeRadius);
        }
    }
}
#endif