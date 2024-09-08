using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NPBehave;
using System.IO;

// for referencing
// smc = Start of my code. emc = end of my code.

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
        // m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        m_PushBlockSettings = Object.FindFirstObjectByType<PushBlockSettings>();
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

        setInitialBlackboardValues();
    }

    // Update is called once per frame
    void Update()
    {
        // MoveAgent();        
    }




    /// based on npbehave exampleswarmai
    private Root CreateBehaviourTree()
    {
        return new Root(ownBlackboard,

            
            // Update values in the blackboards every 125 milliseconds
            new Service(0.125f, UpdateBlackboards,
                    new Selector(
                        new BlackboardCondition("agentCanMove", Operator.IS_EQUAL, true, Stops.NONE,
                        new Sequence(
                            new Action(() =>
                            {
                                if (sharedBlackboard.Get<Vector3>("dragonPosition") != null && (sharedBlackboard.Get<bool>("isKeyPickedUp") != true))
                                {
                                    MoveAgentToDragon(sharedBlackboard.Get<Vector3>("dragonPosition"));
                                }
                            })
                            { Label = "Move to dragon position" }
                        )
                        ),
                        //smc
                        new BlackboardCondition("isKeyPickedUp", Operator.IS_EQUAL, true, Stops.NONE,
                        new Sequence(
                            new Action(() => {
                                if (sharedBlackboard.Get<Vector3>("doorPosition") != null)
                                {
                                    moveAgentTo(sharedBlackboard.Get<Vector3>("doorPosition"));
                                }
                            }) {Label = "Move agent to exit"}
                        ))
                    )
                    //emc
                )
        );
    }

    private void UpdateBlackboards()
    {
        // smc
        // randomly switch the blackboard value
        if (Time.frameCount % 17 == 0)
        {
            ownBlackboard["agentCanMove"] = false;

            //for debugging
            // Debug.Log("number of agents in shared blackboard: " + sharedBlackboard.Get<int>("numberOfAgents"));
        }
        else
        {
            ownBlackboard["agentCanMove"] = true;
        }

        //add/update the dragon Transform to the shared blackboard
        sharedBlackboard["dragonT"] = m_GameController.DragonsList[0].T;

        // update the dragon position
        getDragonPosition();
        //emc
    }

    //=== start my code ===//

    //set values for initial state
    private void setInitialBlackboardValues(){
        sharedBlackboard["isDragonAlive"] = true;

        sharedBlackboard["isKeyPickedUp"] = false;

        //for debugging purposes
        sharedBlackboard["numberOfAgents"] = sharedBlackboard.Get<int>("numberOfAgents") + 1;

        getDoorPosition();
    }

    //get the dragons position
    private void getDragonPosition(){
        //check if dragon is in game
        if (m_GameController.DragonsList[0].T && (sharedBlackboard.Get<bool>("isDragonAlive") == true))
        {
            sharedBlackboard["dragonPosition"] = m_GameController.DragonsList[0].T.position;
        }
    }

    // move the agent towards the dragons position
    public void MoveAgentToDragon(Vector3 vec)
    {
        transform.position = Vector3.MoveTowards(transform.position, vec, Time.deltaTime * 1f);
    }

    // move agent to given vector location
    private void moveAgentTo(Vector3 pos){
        transform.position = Vector3.MoveTowards(transform.position, pos, Time.deltaTime * 50f);
    }

    //position for agents to escape to
    private void getDoorPosition(){
        sharedBlackboard["doorPosition"] = GameObject.FindGameObjectWithTag("lock").transform.position;
    }

    //get dragon killed position
    private void getDragonKilledPosition(Vector3 lastPos){
        sharedBlackboard["dragonKilledPosition"] = lastPos;
    }

    //update dragon game participation status
    private void updateDragonLifeStatus(){
        sharedBlackboard["isDragonAlive"] = false;
    }

    // let other agents know key has been collected
    private void updateAgentHasKeyStatus(){
        sharedBlackboard["isKeyPickedUp"] = true;
    }

    //reset blackboard values when agents escape
    private void resetBlackboardValues()
    {
        sharedBlackboard["isDragonAlive"] = true;
        sharedBlackboard["isKeyPickedUp"] = false;
    }


    /// <summary>
    /// adapted from: https://docs.unity3d.com/2020.1/Documentation/Manual/JSONSerialization.html
    /// </summary>
    [System.Serializable]
    public class JSONGameMetrics
    {
        public float time;
        public float dragonKilledTime;
        public float agentPickedKeyTime;
        public float agentExitTime;
        
    }

    JSONGameMetrics jsonObject = new JSONGameMetrics();


        string path = "/home/yehuda/Desktop/Temp/Unity/json-file-tests/test.json";
    private void writeToFile(){
        //current simulation time
        jsonObject.time = Time.time;
        
        //convert JSON object into string, for writing to a file
        string json = JsonUtility.ToJson(jsonObject);

        // append current JSON data to file
        File.AppendAllText(path, json.ToString());
    }

    //=== end my code ===//

    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false);
                IHaveAKey = false;
                m_GameController.UnlockDoor();

                //smc
                // set time first agent exits
                jsonObject.agentExitTime = Time.time;

                //update blackboard for next iteration of game
                resetBlackboardValues();

                //save JSON data to file
                writeToFile();
                //emc
            }
        }
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col);
            MyKey.SetActive(false);
            IHaveAKey = false;

            //smc
            //update dragons postion for key location 
            // getDragonKilledPosition(col.transform.position);
            getDragonKilledPosition(MyKey.transform.position);

            //get the door position for agents to move towards after collecting the key
            getDoorPosition();

            //update blackboard that dragon has been killed
            updateDragonLifeStatus();


            // set time dragon killed for evaluation
            jsonObject.dragonKilledTime = Time.time;
            //emc
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

            //smc
            //update other agents that key has been picked up
            updateAgentHasKeyStatus();

            // time agent picked up key (after dragon has been killed)
            jsonObject.agentPickedKeyTime = Time.time;      
            //emc      
        }
    }

}
