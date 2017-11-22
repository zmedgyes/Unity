using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ProtocolClient : MonoBehaviour {

    //public Transform self;
    public ProtocolServer server;
    public float bodyRadius = 0.5f * Mathf.Sqrt(2);

    List<Node> path = null;
    public Node target = null;
    Node lastTarget;
    int currentWaypoint;
    Grid grid;

    MovementController controller;
    ManualController manualController;

    public bool autoControlMode = true;

    HD.TCPChat tcpServer;
    int cnt = 0;

    public List<Node> lastWalkable = new List<Node>();
    public List<Node> lastUnwalkable = new List<Node>();

    bool pathRequestPending = false;
    bool targetRequestPending = false;

    bool pathSuccess = false;
    bool targetSuccess = true;
    bool waitAndNewTargetCalled = false;

    bool run = true;

    bool dataSent = false;

    public Transform targetIndicator;

    // Use this for initialization
    void Start () {
        controller = GetComponent<MovementController>();
        manualController = GetComponent<ManualController>();
        lastTarget = target;
        //self = transform;
        server.clients.Add(this);
        grid = server.grid;
        tcpServer = GetComponent<HD.TCPChat>();
    }

    // Update is called once per frame
    void Update() {
        controller.controllEnabled = autoControlMode;
        manualController.controllEnabled = !autoControlMode;
        if (autoControlMode)
        {
            uploadControllInfo(controller.lastMovementSpeed, controller.lastRotAngle);
        }
        else
        {
            uploadControllInfo(manualController.lastMovementSpeed, manualController.lastRotAngle);
        }

        if (tcpServer)
        {
            if (cnt > 10)
            {
                tcpServer.Send("testmessage");
                cnt = 0;
            }
            cnt++;
        }

        if (run)
        {
            uploadSensorDataToServer();

            //ha nem( útra várás || targetre várás || nincs elérhető target)
            if (!(pathRequestPending || targetRequestPending || !targetSuccess))
            {
                //ha nem indulás
                if (path != null)
                {

                    //ha új target van
                    if (!target.Equals(lastTarget))
                    {
                        getNewPath(target);
                        lastTarget = target;
                    }
                    //ha nincs új target
                    else {
                        //ha rossz a jelenlegi út 
                        if (!checkPath())
                        {
                            if (wrongPosition())
                            {
                                rescue();
                            }
                            if (!pathSuccess)
                            {
                                getNewTarget(); //ha nem létezik út a jelenlegi célhoz
                            }
                            else {
                                getNewPath(path[path.Count - 1]); //ha létezik út a jelenlegi célhoz
                            }
                        }
                        //ha jó a jelenlegi út
                        else {
                            controller.moveTo(path[currentWaypoint].worldPosition);

                            //ha elértük az aktuális waypointot
                            if (waypointReached())
                            {
                                //ha az elért pont a target
                                if (path.Count.Equals(currentWaypoint + 1))
                                {
                                    getNewTarget();
                                }
                                else {
                                    currentWaypoint++;
                                    controller.moveTo(path[currentWaypoint].worldPosition);
                                }
                            }
                        }
                    }
                }
                //ha indulás
                else {
                    //ha van már target
                    if (target != null)
                    {
                        getNewPath(target);
                        lastTarget = target;
                    }
                    //ha még nincs target
                    else {
                        if (dataSent)
                            getNewTarget();
                    }
                }
            }
            //ha nincs elérhető target
            if (!targetSuccess)
            {
                //ha nem hítunk még új target-et   
                if (!waitAndNewTargetCalled)
                {
                    waitAndNewTargetCalled = true;
                    //5mp várás, utána új target keresése
                    StartCoroutine(WaitAndNewPath(5));
                }
            }
        }
	}

    //a hátralévő út járhatóságának ellenőrzése
    bool checkPath() {
        for (int i = currentWaypoint; i < path.Count; i++)
        {
            if (path[i].danger > 0 || server.getDynamicUnwalkable(this,2).Contains(path[i])){
                return false;
            }
        }
        return true;
    }

    //új útvonal igénylése adott target-hez
    void getNewPath(Node target){
        pathRequestPending = true;
        server.requestNewPath(this, target, OnPathFound);
    }

    //új target igénylése
    void getNewTarget(){
        targetRequestPending = true;
        server.requestNewTarget(this, OnTargetFound);
    }

    //ellenőrzi, hogy elértük-e az aktuális waypoint-ot az útvonalon
    bool waypointReached(){
        int i = 0;
        for (i=0; i < path.Count; i++)
        {
            if (grid.NodeFromWorldPoint(transform.position).Equals(path[i]))
            {
                currentWaypoint = i;
                return true;
            }
        }
        if (grid.NodeFromWorldPoint(transform.position).Equals(path[currentWaypoint])){
            return true;
        }
        return false;
    }

    //sensoradatok feltöltése a serverre
    void uploadSensorDataToServer(){
        if ((lastUnwalkable.Count + lastWalkable.Count) > 0)
        {
            dataSent = true;
        }
        lastWalkable.AddRange(grid.nodesInRadius(transform.position, 0.5f));
        server.uploadSensorData(lastWalkable, lastUnwalkable,this);
        lastWalkable.Clear();
        lastUnwalkable.Clear();
        
    }

    //nyers sensoradatok
    public void rawSensorData(Vector3 center, Vector3 dir, float angle, float radius, HashSet<Vector3> hitPoints)
    {
        //TODO
        QuadTree grid = new QuadTree();


        foreach (Vector3 point in hitPoints)
        {
            grid.insertUnwalkable(point);
        }
    }

    private byte[] packetMessge(byte[] buffer) {
        byte[] len = BitConverter.GetBytes(buffer.Length + 4);
        Array.Reverse(len);

        byte[] rv = new byte[len.Length + buffer.Length];
        System.Buffer.BlockCopy(len, 0, rv, 0, len.Length);
        System.Buffer.BlockCopy(buffer, 0, rv, len.Length, buffer.Length);
        return rv;
    }
    
    public void uploadSensorMeta(float distance, float angle, int step)
    {
        byte[] msgType = { 1 };
        byte[] dst = BitConverter.GetBytes(distance);
        Array.Reverse(dst);
        byte[] ang = BitConverter.GetBytes(angle);
        Array.Reverse(ang);
        byte[] stp = BitConverter.GetBytes(step);
        Array.Reverse(stp);

        byte[] rv = new byte[dst.Length + ang.Length + stp.Length +msgType.Length];
        System.Buffer.BlockCopy(msgType, 0, rv, 0, msgType.Length);
        System.Buffer.BlockCopy(dst, 0, rv, msgType.Length, dst.Length);
        System.Buffer.BlockCopy(ang, 0, rv, dst.Length + msgType.Length, ang.Length);
        System.Buffer.BlockCopy(stp, 0, rv, ang.Length + dst.Length + msgType.Length, stp.Length);

        if (tcpServer) {
            byte[] msg = packetMessge(rv);
            print("DIST: " + distance);
            print("ANGLE: " + angle);
            print("STEP: " + step);
        }
        
    }
    public void uploadSensorData(List<float> hitDistances)
    {
        byte[] msgType = { 2 };
        byte[] len = BitConverter.GetBytes(hitDistances.Count);
        byte[] items = new byte[hitDistances.Count * sizeof(float)];
        for (var i = 0; i < hitDistances.Count; i++) {
            byte[] tmp = BitConverter.GetBytes(hitDistances[i]);
            Array.Reverse(tmp);
            System.Buffer.BlockCopy(tmp, 0, items, i*sizeof(float), msgType.Length);
        }
        byte[] rv = new byte[len.Length + items.Length + msgType.Length];
        System.Buffer.BlockCopy(msgType, 0, rv, 0, msgType.Length);
        System.Buffer.BlockCopy(len, 0, rv, msgType.Length, len.Length);
        System.Buffer.BlockCopy(items, 0, rv, len.Length + msgType.Length, items.Length);
        if (tcpServer)
        {
            byte[] msg = packetMessge(rv);
            print("COUNT: " + hitDistances.Count);
        }

    }
    public void uploadControllInfo(float speed, float angle) {
        byte[] msgType = { 3 };
        byte[] spd = BitConverter.GetBytes(speed);
        Array.Reverse(spd);
        byte[] ang = BitConverter.GetBytes(angle);
        Array.Reverse(ang);
        byte[] rv = new byte[spd.Length + ang.Length + msgType.Length];
        System.Buffer.BlockCopy(msgType, 0, rv, 0, msgType.Length);
        System.Buffer.BlockCopy(spd, 0, rv, msgType.Length, spd.Length);
        System.Buffer.BlockCopy(ang, 0, rv, spd.Length + msgType.Length, ang.Length);

        if (tcpServer)
        {
            byte[] msg = packetMessge(rv);
            print("SPEED: " + speed);
            print("ANGLE: " + angle);
        }
    }
    public void uploadStringLog(string log) {
        byte[] msgType = { 4 };
        byte[] buffer = System.Text.Encoding.BigEndianUnicode.GetBytes(log);
        byte[] len = BitConverter.GetBytes(buffer.Length);
        Array.Reverse(len);
        byte[] rv = new byte[len.Length + buffer.Length+msgType.Length];
        System.Buffer.BlockCopy(msgType, 0, rv, 0, msgType.Length);
        System.Buffer.BlockCopy(len, 0, rv, msgType.Length, len.Length);
        System.Buffer.BlockCopy(buffer, 0, rv, len.Length+msgType.Length, buffer.Length);

        if (tcpServer)
        {
            byte[] msg = packetMessge(rv);
            print("LOG: " + log);
        }
    }

    //!! új útvonal elkészülése esetén hívott fgv
    public void OnPathFound(List<Node> newPath, bool pathSuccessful)
    {
        pathRequestPending = false;
        pathSuccess = pathSuccessful;
        if (pathSuccessful){
            path = newPath;
            currentWaypoint = 0;
        }
    }

    //!! új target elkészülése után hívott fgv
    public void OnTargetFound(Node newTarget, bool targetSuccessful)
    {
        waitAndNewTargetCalled = false;
        targetRequestPending = false;
        targetSuccess = targetSuccessful;
        if (targetSuccessful){
            target = newTarget;
            if (targetIndicator != null)
            {
                targetIndicator.position = target.worldPosition;
            }
            uploadStringLog("new Target");
        }
    }

    //megállapítja, hogy most járhatatlanan mezőn állunk-e
    bool wrongPosition()
    {
        Node currentNode = grid.NodeFromWorldPoint(transform.position);
        if (currentNode.danger>0 || !currentNode.walkable)
        {
            return true;
        }
        return false;
    }

    //visszavezet az útra
    void rescue()
    {
        int i;
        for (i = currentWaypoint; i < path.Count; i++)
        {
            if(path[i].danger==0 && path[i].walkable)
            {
                controller.moveTo(path[i].worldPosition);
                currentWaypoint = i;
                break;
            }
        }
    }

    public bool getTargetSuccess()
    {
        return targetSuccess;
    }

    public void turnOff() {
        run = false;
    }

    public void turnOn()
    {
        run = true;
    }

    //5mp várá utána új target igénylése
    IEnumerator WaitAndNewPath(int sec)
    {
            yield return new WaitForSeconds(sec);
            getNewTarget();
    }

    //hátralévő útvonal kirajzolása
    public void OnDrawGizmos()
    {
        if (path != null)
        {
            for (int i = currentWaypoint; i < path.Count; i++)
            {
                Gizmos.color = Color.black;

                if (i == currentWaypoint)
                {
                    Gizmos.DrawLine(transform.position, path[i].worldPosition);
                }
                else {
                    Gizmos.DrawLine(path[i - 1].worldPosition, path[i].worldPosition);
                }

            }
        }
    }
}

