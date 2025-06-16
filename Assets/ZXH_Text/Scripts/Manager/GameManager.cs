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
    public GameObject spawnPointObj;        // ���볡���еĳ�������� (SpawnPoint)

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
        //������
        if (spawnPointObj != null && pathSpline != null)
        {
            spawnPointObj.transform.position = pathSpline.Spline.EvaluatePosition(0f);
        }

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

        //tempChain.Reverse();
        marbleChain = tempChain;
        RecalculateAllDistances();

        // ������Ϻ󣬽���һ��ȫ�ּ�飬������ʼ�ʹ��ڵĳ���
        yield return new WaitForEndOfFrame(); // �ȴ�һ֡��ȷ������λ�ö��Ѹ���
        CheckAndEliminateAllMatches();
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

    // ... (������������ InsertMarble, CheckForMatches �����·�) ...

    /// <summary>
    /// ���뷢������ӵ������У���ײʱ����X�����жϲ���ǰ�󣬲��Զ�����������
    /// </summary>
    /// <param name="collisionMarble">����ײ����������</param>
    /// <param name="shotMarble">��������ӣ���ʵ������</param>
    public void InsertMarble(Marble collisionMarble, Marble shotMarble)
    {
        // ��ȡ��ײ�����������е�����
        int collisionIndex = marbleChain.IndexOf(collisionMarble);
        if (collisionIndex == -1) return; // �����Լ��

        // �жϲ��뵽ǰ�滹�Ǻ���
        int insertIndex = collisionIndex;
        if (shotMarble.transform.position.x > collisionMarble.transform.position.x)
        {
            // ���������Ҳ࣬���뵽����
            insertIndex = collisionIndex + 1;
        }
        // ������뵽ǰ�棨insertIndex = collisionIndex��

        // ���������Ӳ��뵽����
        marbleChain.Insert(insertIndex, shotMarble);

        // �����������Ⲣ�������г��֧��������
        CheckAndEliminateAllMatches();
    }

    /// <summary>
    /// ��鲢�������п���ɳ�������ӣ�֧����������
    /// </summary>
    public void CheckAndEliminateAllMatches()
    {
        bool foundMatch;
        do
        {
            foundMatch = false;
            for (int i = 0; i <= marbleChain.Count - 4; i++)
            {
                string idiom = "";
                for (int j = 0; j < 4; j++)
                {
                    idiom += marbleChain[i + j].GetCharacter();
                }
                if (_idiomData.IsValidSessionIdiom(idiom))
                {
                    // ��������
                    for (int k = 0; k < 4; k++)
                    {
                        Destroy(marbleChain[i].gameObject); // ÿ�ζ��Ƴ�i����Ϊ����Ļ�ǰ��
                        marbleChain.RemoveAt(i);
                    }
                    foundMatch = true;
                    break; // ���´�ͷ��⣬֧������
                }
            }
        } while (foundMatch);
    }

    /// <summary>
    /// [�����߼�] ���ָ��λ�ø����Ƿ�����ɳ���
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
    /// [����API] �ṩ�� Launcher ��ȡ��һ��Ҫ������֡�
    /// </summary>
    public char GetNextCharForLauncher()
    {
        return _idiomData.GetNextCharacterToShoot();
    }

    /// <summary>
    /// ��ȡָ�����������е�����λ��
    /// </summary>
    /// <param name="marble">����</param>
    /// <returns></returns>
    public int GetMarbleIndex(Marble marble)
    {
        return marbleChain.IndexOf(marble);
    }


    /// <summary>
    /// ����һ������������ڹ���ϵ�������·�̾���
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