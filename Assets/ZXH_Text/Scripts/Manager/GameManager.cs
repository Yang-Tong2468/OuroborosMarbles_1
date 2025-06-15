// GameManager.cs
// ְ����Ϸ���̵��ܿ������ġ����������������ɡ��ƶ��������������

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using System.Collections;
using Unity.Mathematics; // ȷ��ʹ����ȷ�� Spline �����ռ�

public class GameManager : MonoBehaviour
{
    // [����ģʽ] ���������ű�����Launcher������
    public static GameManager Instance;

    [Header("������Դ����")]
    public TextAsset idiomFile;          // ���� chengyu.txt
    public GameObject marblePrefab;      // ���� Marble Prefab
    public SplineContainer pathSpline;   // ���볡���еĹ������ (Path)

    [Header("�ؿ�����Ϸ����")]
    public int idiomsPerLevel = 10;      // ����ʹ�ö��ٸ�����
    public float marbleSpeed = 1f;       // �������Ļ����ƶ��ٶ�
    public int initialMarbleCount = 20; // ��һ���ܹ����ɵ���������
    public float spawnAnimationSpeed = 0.1f; // �������ɵĶ����ٶȣ�ֵԽСԽ��

    // [���ݹ�����] ����������Ƶ� IdiomDataManager
    private IdiomDataManager _idiomData;

    // [�������ݽṹ] �洢�������������ӵĶ�̬�б�
    [SerializeField]private List<Marble> marbleChain = new List<Marble>();

    void Awake()
    {
        // ���õ���
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // [�ؼ���ʼ��] ����Ϸ��ʼʱ����������ʼ�����ݹ�����
        _idiomData = new IdiomDataManager(idiomFile, idiomsPerLevel);
    }

    void Start()
    {
        // [�ؼ��޸�] ֱ���Թ�����Ϊ��׼����
        // [�ؼ��޸�] ʹ��Э������̬�������ӣ����⾺̬���������Ӷ���Ч��
        StartCoroutine(SpawnInitialChainCoroutine(initialMarbleCount));
    }

    /// <summary>
    /// [�����ƶ��߼�] �ڹ̶���ʱ����ִ�У���ʵ��ƽ��������ȷ���ƶ���
    /// </summary>
    void FixedUpdate()
    {
        if (marbleChain.Count == 0) return;

        // --- ���� 1: ����ÿ�����ӵġ�����·�̾��롱 (Target Distance) ---
        List<float> targetDistances = CalculateTargetDistances();

        // --- ���� 2: ���ݡ�����λ�á����� Rigidbody2D ���ٶ� ---
        DriveMarblesToTargets(targetDistances);
    }

    /// <summary>
    /// ����ѧ���㣬�ó���֡ÿ������Ӧ���ڵľ�ȷ·�̾��롣
    /// </summary>
    private List<float> CalculateTargetDistances()
    {
        List<float> distances = new List<float>(marbleChain.Count);
        if (marbleChain.Count == 0) return distances;

        // ͷ������������ǵ�ǰ�������һС��λ��
        distances.Add(marbleChain[0].distanceOnPath + marbleSpeed * Time.fixedDeltaTime);

        // �������и����ߵ��������
        for (int i = 1; i < marbleChain.Count; i++)
        {
            float targetSpacing = (marbleChain[i - 1].Diameter + marbleChain[i].Diameter) * 0.5f;
            float followerTargetDistance = distances[i - 1] - targetSpacing;

            // Ϊ��ֹ������ֻ�е����ӱ�����ʱ�Ÿ�������Ŀ��λ��
            distances.Add(Mathf.Max(followerTargetDistance, marbleChain[i].distanceOnPath));
        }
        return distances;
    }

    /// <summary>
    /// Ϊÿ�����Ӹ���һ���ٶȣ�ʹ��ƽ���س��Լ���Ŀ��λ���ƶ���
    /// </summary>
    private void DriveMarblesToTargets(List<float> targetDistances)
    {
        for (int i = 0; i < marbleChain.Count; i++)
        {
            Marble marble = marbleChain[i];
            marble.distanceOnPath = targetDistances[i]; // ���������Լ��ľ����¼

            float posNormalized = pathSpline.Spline.ConvertIndexUnit(marble.distanceOnPath, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            Vector3 targetPosition = pathSpline.EvaluatePosition(posNormalized);

            // ���Ļ���㷨���ٶ� = (Ŀ��λ�� - ��ǰλ��) / ʱ��
            Vector3 movementVector = (targetPosition - marble.transform.position);
            marble.GetComponent<Rigidbody2D>().velocity = movementVector / Time.fixedDeltaTime;

            // �������
            float distanceMoved = marble.GetComponent<Rigidbody2D>().velocity.magnitude * Time.fixedDeltaTime;
            marble.UpdateRotation(distanceMoved);

            float3 tangentFloat3 = pathSpline.Spline.EvaluateTangent(posNormalized);
            marble.transform.up = Vector3.Cross(new Vector2(tangentFloat3.x, tangentFloat3.y), Vector3.forward);
        }
    }

    /// <summary>
    /// [�����߼�] ʹ��Э�̣�����Ϸ��ʼʱ����������ӣ��γɶ���Ч����
    /// </summary>
    private IEnumerator SpawnInitialChainCoroutine(int count)
    {
        foreach (var m in marbleChain) if (m != null) Destroy(m.gameObject);
        marbleChain.Clear();

        float totalOffset = 0f;
        List<Marble> tempChain = new List<Marble>();

        for (int i = 0; i < count; i++)
        {
            char nextChar = _idiomData.GetNextCharacterForInitialSpawn();
            if (nextChar == '?') break;

            GameObject newMarbleObj = Instantiate(marblePrefab, pathSpline.Spline.EvaluatePosition(0), Quaternion.identity);
            Marble newMarble = newMarbleObj.GetComponent<Marble>();
            newMarble.SetCharacter(nextChar);

            float currentOffset = (i > 0) ? (tempChain[i - 1].Diameter + newMarble.Diameter) * 0.5f : newMarble.Diameter * 0.5f;
            totalOffset += currentOffset;
            newMarble.distanceOnPath = totalOffset;

            tempChain.Add(newMarble);
            yield return null;
        }

        tempChain.Reverse();
        marbleChain = tempChain;
        RecalculateAllDistances();
    }



    /// <summary>
    /// ����������ת�����¼����������ӵ�·�̾��룬��ȷ��˳����ȷ��
    /// </summary>
    private void RecalculateAllDistances()
    {
        if (marbleChain.Count == 0) return;
        float headDistance = marbleChain[0].distanceOnPath; // �Է�ת���ͷ��Ϊ��׼

        for (int i = 1; i < marbleChain.Count; i++)
        {
            float spacing = (marbleChain[i - 1].Diameter + marbleChain[i].Diameter) * 0.5f;
            headDistance -= spacing;
            marbleChain[i].distanceOnPath = headDistance;
        }
    }



    //void Update()
    //{
    //    // [����ѭ��] ÿ֡���ø�Ч���ƶ�����
    //    MoveMarbleChainSmoothly();
    //}

    // Fix for CS1061: Replace the usage of `GetDistance` with a valid method or calculation.  
    // Based on the provided Spline type signatures, `GetCurveLength` can be used to calculate distances.  

    //private void MoveMarbleChainSmoothly()
    //{
    //    Debug.Log("MoveMarbleChainSmoothly called. Chain count: " + marbleChain.Count);

    //    if (marbleChain.Count == 0) return;

    //    // 1. �ƶ��쵼�� (�����ĵ�һ������)  
    //    Marble headMarble = marbleChain[0];
    //    Vector3 oldPos = headMarble.transform.position;

    //    //ʹ�������µĸ�����������ȡ����
    //    float oldDistanceOnPath = GetDistanceOnSpline(headMarble.transform.position);  

    //    float newDistanceOnPath = oldDistanceOnPath + marbleSpeed * Time.deltaTime;

    //    // �����쵼�ߵ�λ�ú���̬ (ʹ�䴹ֱ�ڹ������)  
    //    float newPosNormalized = pathSpline.Spline.ConvertIndexUnit(newDistanceOnPath, PathIndexUnit.Distance, PathIndexUnit.Normalized);
    //    headMarble.transform.position = pathSpline.EvaluatePosition(newPosNormalized);
    //    Vector3 newPos = headMarble.transform.position;
    //    Vector3 forward = (Vector3)pathSpline.EvaluateTangent(newPosNormalized);
    //    headMarble.transform.up = Vector3.Cross(forward, Vector3.forward);

    //    // �����쵼�ߵĹ���Ч��  
    //    headMarble.UpdateRotation(newPos - oldPos);

    //    // 2. �ƶ����и����ߣ������ǽ���ǰ��  
    //    for (int i = 1; i < marbleChain.Count; i++)
    //    {
    //        Marble currentMarble = marbleChain[i];
    //        Marble marbleInFront = marbleChain[i - 1];

    //        // Ŀ�꣺��ǰ���ӵ����ĵ���ǰһ�����ӵ����ĵ㱣��һ����ֱ���͵�һ�롱�ľ���  
    //        float targetSpacing = (marbleInFront.Diameter + currentMarble.Diameter) * 0.5f;
    //        float frontMarbleDistance = GetDistanceOnSpline(marbleInFront.transform.position);   
    //        float targetDistance = frontMarbleDistance - targetSpacing;

    //        //float currentDistance = GetDistanceOnSpline(headMarble.transform.position); 
    //        Vector3 oldFollowerPos = currentMarble.transform.position;

    //        // ֱ�����õ�Ŀ��λ�ã�Ҳ������ Lerp ��һ��΢С��ƽ������Ч��  
    //        float posNormalized = pathSpline.Spline.ConvertIndexUnit(targetDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
    //        currentMarble.transform.position = pathSpline.EvaluatePosition(posNormalized);
    //        Vector3 newFollowerPos = currentMarble.transform.position;
    //        forward = (Vector3)pathSpline.EvaluateTangent(posNormalized);
    //        currentMarble.transform.up = Vector3.Cross(forward, Vector3.forward);

    //        // ���¸����ߵĹ���Ч��  
    //        currentMarble.UpdateRotation(newFollowerPos - oldFollowerPos);
    //    }
    //}

    private void MoveMarbleChainSmoothly()
    {
        if (marbleChain.Count == 0) return;

        // --- 1. ����ͷ��(Head Marble)���ٶ� ---
        Marble headMarble = marbleChain[0];
        Rigidbody2D headRb = headMarble.GetComponent<Rigidbody2D>();

        // ��ȡͷ���ڹ���ϵĹ�һ��λ�� 't'
        SplineUtility.GetNearestPoint(pathSpline.Spline, headMarble.transform.position, out _, out float t);

        // ��ȡ�õ�����߷��� (��ǰ������)
        // ��ȡ��һ���� float3 ��������
        float3 tangentFloat3 = math.normalize(pathSpline.Spline.EvaluateTangent(t));
        // �ֶ����� Vector2��ֻʹ�� x �� y ����
        Vector2 tangent = new Vector2(tangentFloat3.x, tangentFloat3.y);

        // ͷ�����ٶȾ��ǻ����ٶȳ��Է���
        Vector2 headVelocity = tangent * marbleSpeed;
        headRb.velocity = headVelocity;

        // �����ٶȸ�����ת
        //headMarble.UpdateRotation(headVelocity);

        // --- 2. �������и�����(Follower Marbles)���ٶ� ---
        for (int i = 1; i < marbleChain.Count; i++)
        {
            Marble currentMarble = marbleChain[i];
            Marble marbleInFront = marbleChain[i - 1];
            Rigidbody2D currentRb = currentMarble.GetComponent<Rigidbody2D>();

            // Ŀ�꣺�����Ӽ�ľ��뱣��Ϊһ������ֵ (�뾶֮��)
            float targetSpacing = (marbleInFront.Diameter + currentMarble.Diameter) * 0.5f;
            float currentSpacing = Vector2.Distance(currentMarble.transform.position, marbleInFront.transform.position);

            // ����������
            float distanceError = currentSpacing - targetSpacing;

            // [�����㷨������ģ��]
            // ���ݾ�������������ٶȣ��γ�һ�������ɡ�Ч��
            // �������̫Զ (error > 0)���ͼ���׷��
            // �������̫�� (error < 0)���ͼ���������������������
            // correctionFactor �����˵��ɵġ�Ӳ�ȡ�
            float correctionFactor = 5f;
            float speedAdjustment = distanceError * correctionFactor;

            // �����ߵ��ٶ� = �����ٶ� + �����ٶ�
            float followerSpeed = marbleSpeed + speedAdjustment;

            // ȷ���ٶȲ��������Ϊ������������Ҫ���ˣ�
            followerSpeed = Mathf.Clamp(followerSpeed, 0, marbleSpeed * 2);

            // ��ȡ�����ߵ�ǰλ�õ����߷���
            SplineUtility.GetNearestPoint(pathSpline.Spline, currentMarble.transform.position, out _, out float follower_t);
            float3 followerTangentFloat3 = math.normalize(pathSpline.Spline.EvaluateTangent(follower_t));
            Vector2 followerTangent = new Vector2(followerTangentFloat3.x, followerTangentFloat3.y);

            // ���ø����ߵ������ٶ�
            Vector2 followerVelocity = followerTangent * followerSpeed;
            currentRb.velocity = followerVelocity;

            // �����ٶȸ�����ת
            //currentMarble.UpdateRotation(followerVelocity);
        }
    }

    // ... (������������ InsertMarble, CheckForMatches �����·�) ...

    /// <summary>
    /// [ƽ�������߼�] �������������ײ���ڴ˴�ִ�в��������
    /// </summary>
    public void InsertMarble(int collisionIndex, Marble shotMarble)
    {
        // ʵ���������ӣ���������������
        GameObject newMarbleObj = Instantiate(marblePrefab, shotMarble.transform.position, Quaternion.identity);
        Marble newMarble = newMarbleObj.GetComponent<Marble>();
        newMarble.SetCharacter(shotMarble.GetCharacter());

        // �������Ӳ��뵽 List �е���ȷλ��
        int insertionIndex = collisionIndex + 1;
        marbleChain.Insert(insertionIndex, newMarble);

        // **���ٵ���ȫ�ָ��£�** �ƶ�ѭ��������һ֡�Զ������������ƽ���ƿ���

        // �������������Ƿ�����ɳ���
        CheckForMatches(insertionIndex);
    }

    /// <summary>
    /// [�����߼�] ���ָ��λ�ø����Ƿ�����ɳ��
    /// </summary>
    public void CheckForMatches(int insertionIndex)
    {
        // ����Բ����Ϊ���ĵ�����4�����ܵ��������
        for (int i = 0; i < 4; i++)
        {
            int startIndex = insertionIndex - i;
            if (startIndex < 0 || startIndex + 3 >= marbleChain.Count) continue;

            string potentialIdiom = "";
            for (int j = 0; j < 4; j++)
            {
                potentialIdiom += marbleChain[startIndex + j].GetCharacter();
            }

            // [�ؼ�] ʹ�����ݹ���������֤����
            if (_idiomData.IsValidSessionIdiom(potentialIdiom))
            {
                Debug.Log("ƥ��ɹ�: " + potentialIdiom);
                // �Ӻ���ǰɾ����������������
                for (int k = 0; k < 4; k++)
                {
                    Destroy(marbleChain[startIndex + 3 - k].gameObject);
                    marbleChain.RemoveAt(startIndex + 3 - k);
                }

                // TODO: �ڴ˿������������������Ч��
                break; // �ҵ�һ�������˳�
            }
        }
    }

    /// <summary>
    /// [�����ع� V2.0] �Թ�����Ϊ��׼���������ָ�����������ӡ�
    /// </summary>
    /// <param name="count">Ҫ���ɵ���������</param>
    //private IEnumerator SpawnInitialChainCoroutine(int count)
    //{
    //    Debug.Log("Spawning coroutine started...");

    //    // ��վ�����
    //    foreach (var marble in marbleChain) Destroy(marble.gameObject);
    //    marbleChain.Clear();

    //    for (int i = 0; i < count; i++)
    //    {
    //        // 1. �����ݹ�������ȡ��һ����
    //        char nextChar = _idiomData.GetNextCharacterForInitialSpawn();
    //        if (nextChar == '?') break;

    //        // 2. ʵ����������
    //        GameObject newMarbleObj = Instantiate(marblePrefab);
    //        Marble newMarble = newMarbleObj.GetComponent<Marble>();
    //        newMarble.SetCharacter(nextChar);

    //        // 3. �������ӷ����ڹ����� (distance 0)
    //        // ��������Ψһְ��
    //        float posNormalized = pathSpline.Spline.ConvertIndexUnit(0, PathIndexUnit.Distance, PathIndexUnit.Normalized);
    //        newMarble.transform.position = pathSpline.EvaluatePosition(posNormalized);

    //        // 4. ����������ӵ������ġ�ͷ����������0��
    //        marbleChain.Insert(0, newMarble);

    //        // 5. �ȴ�һС��ʱ�䣬�Կ������ɵ��ٶ�
    //        yield return new WaitForSeconds(spawnAnimationSpeed);
    //    }
    //}

    //private IEnumerator SpawnInitialChainCoroutine(int count)
    //{
    //    foreach (var marble in marbleChain) Destroy(marble.gameObject);
    //    marbleChain.Clear();

    //    for (int i = 0; i < count; i++)
    //    {
    //        char nextChar = _idiomData.GetNextCharacterForInitialSpawn();
    //        if (nextChar == '?') break;

    //        // ʵ�������ӣ��������ڹ�����
    //        GameObject newMarbleObj = Instantiate(marblePrefab, pathSpline.Spline.EvaluatePosition(0), Quaternion.identity);
    //        Marble newMarble = newMarbleObj.GetComponent<Marble>();
    //        newMarble.SetCharacter(nextChar);

    //        // ����������ӵ�������ͷ��������0��
    //        // ��һ���ǹؼ���������Ϊ�µġ�ͷ����
    //        marbleChain.Insert(0, newMarble);

    //        // �ȴ�һС��ʱ��
    //        yield return new WaitForSeconds(spawnAnimationSpeed);
    //    }
    //}


    /// <summary>
    /// [����API] �ṩ�� Launcher ��ȡ��һ��Ҫ������֡�
    /// </summary>
    public char GetNextCharForLauncher()
    {
        return _idiomData.GetNextCharacterToShoot();
    }

    // �� GameManager.cs ����������������
    public int GetMarbleIndex(Marble marble)
    {
        return marbleChain.IndexOf(marble);
    }

    // �� GameManager.cs �ű��ڲ����κ���������֮�����

    /// <summary>
    /// [���ĸ�������] ����һ������������ڹ���ϵ�������·�̾��롣
    /// </summary>
    /// <param name="worldPosition">Ҫ��ѯ����������</param>
    /// <returns>�ӹ����㵽�������ľ���</returns>
    private float GetDistanceOnSpline(Vector3 worldPosition)
    {
        // ����1: ʹ�� SplineUtility.GetNearestPoint �ҵ���������������ĵ㡣
        // �������������ܶ���Ϣ����������Ҫ���� 't'���������һ����·��λ�� (0 to 1)��
        SplineUtility.GetNearestPoint(pathSpline.Spline, worldPosition, out var nearestPoint, out float t);

        // ����2: ����һ����λ�� 't' ת����ʵ�ʵ�·�̾��롣
        return pathSpline.Spline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance);
    }


}