using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NPBehave;


//BT version attempt 
public class PushAgentEscape : MonoBehaviour
{
    public GameObject MyKey; //my key gameobject. will be enabled when key picked up.
    public bool IHaveAKey; //have i picked up a key
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;
    private DungeonEscapeEnvController m_GameController;

    //npbehave 
    private Blackboard sharedBlackboard;
    private Blackboard ownBlackboard;
    private Root behaviorTree;

    // Start is called before the first frame update
    void Start()
    {
        m_GameController = GetComponentInParent<DungeonEscapeEnvController>();
        m_AgentRb = GetComponent<Rigidbody>();
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        MyKey.SetActive(false);
        IHaveAKey = false;

        //the following code is adapted from npbehave swarm-ai example
        sharedBlackboard = UnityContext.GetSharedBlackboard("multi-agent-bt-ai");

        // create a new blackboard instance for this ai instance, parenting it to the sharedBlackboard.
        // This way we can also access shared values through the own blackboard.
        ownBlackboard = new Blackboard(sharedBlackboard, UnityContext.GetClock());

        // create the behaviourTree
        behaviorTree = CreateBehaviourTree();

        // start the behaviour tree
        behaviorTree.Start();
    }


    /// npbehave exampleswarmai
    private Root CreateBehaviourTree()
    {
        return new Root(ownBlackboard,

            // Update values in the blackboards every 125 milliseconds
            new Service(0.125f, UpdateBlackboards,
                    new Selector(
                        new BlackboardCondition("agentCanMove", Operator.IS_EQUAL, true, Stops.BOTH,
                        new Sequence(
                            new Action(() =>
                            {
                                if (sharedBlackboard.Get<Vector3>("dragonPosition") != null)
                                {
                                    MoveAgentToDragon(sharedBlackboard.Get<Vector3>("dragonPosition"));
                                }
                            })
                            { Label = "Move to dragon position" }
                        )
                        )
                    )
                )
        );
    }

    private void UpdateBlackboards()
    {
        // randomly switch the blackboard value
        if (Time.frameCount % 17 == 0)
        {
            ownBlackboard["agentCanMove"] = false;
        }
        else
        {
            ownBlackboard["agentCanMove"] = true;
        }

        //add/update the dragon Transform to the shared blackboard
        sharedBlackboard["dragonT"] = m_GameController.DragonsList[0].T;

        // update the dragon position
        if (m_GameController.DragonsList[0].T)
        {
            sharedBlackboard["dragonPosition"] = m_GameController.DragonsList[0].T.position;
        }
    }

    // move the agent towards the dragons position
    public void MoveAgentToDragon(Vector3 vec)
    {
        transform.position = Vector3.MoveTowards(transform.position, vec, Time.deltaTime * 1f);
    }


    // Update is called once per frame
    void Update()
    {
        // MoveAgent();        
    }

    public void MoveAgent()
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        dirToGo = transform.forward * 1f;

        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }


    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false);
                IHaveAKey = false;
                m_GameController.UnlockDoor();
            }
        }
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col);
            MyKey.SetActive(false);
            IHaveAKey = false;
        }
        if (col.transform.CompareTag("portal"))
        {
            m_GameController.TouchedHazard(this);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        //if we find a key and it's parent is the main platform we can pick it up
        if (col.transform.CompareTag("key") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            print("Picked up key");
            MyKey.SetActive(true);
            IHaveAKey = true;
            col.gameObject.SetActive(false);
        }
    }
}
