using UnityEngine;
using System.Collections.Generic; // ʹ�� List
using System.IO;                  // ���ڶ�ȡ�ļ�
using UnityEngine.Splines;
using System.Collections;// ʹ�� Spline


public class MarbleManager : MonoBehaviour
{
    public static MarbleManager Instance; // ����ģʽ�����������ű�����

    [Header("������Դ")]
    public TextAsset idiomFile;          // ������� chengyu.txt �ļ�
    public GameObject marblePrefab;      // ������� Marble Prefab
    public SplineContainer pathSpline;   // ���볡���е� Path ����

    [Header("��Ϸ����")]
    public float marbleSpeed = 1f;
    public float marbleSpacing = 0.5f; // ����֮��ľ���

    [Header("���ݴ洢")]
    private HashSet<string> idiomDictionary = new HashSet<string>();
    private List<Marble> marbleChain = new List<Marble>();
    private List<string> idiomList = new List<string>(); // ���г���
    private List<char> allCharacters = new List<char>(); // ���г������
    private List<char> shuffledCharacters = new List<char>(); // ���Һ����


    void Awake()
    {
        // ���õ���
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        LoadIdioms();
    }

    void Start()
    {
        // ��Ϸ��ʼʱ�����ɳ�ʼ��һ������
        StartCoroutine(SpawnInitialMarbles());


    }

    void Update()
    {
        // ÿ֡�ƶ�������
        MoveMarbleChain();
    }

    void LoadIdioms()
    {
        if (idiomFile == null)
        {
            Debug.LogError("����ʵ��ļ�δ����!");
            return;
        }

        string[] lines = idiomFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Debug.Log($"���س����ļ����� {lines.Length} �С�");
        foreach (string line in lines)
        {
            string idiom = line.Trim();
            if (idiom.Length == 4)
            {
                idiomDictionary.Add(idiom);
                idiomList.Add(idiom);
                foreach (char c in idiom)
                    allCharacters.Add(c);
            }
        }
        Debug.Log($"������ {idiomDictionary.Count} �����");

        // ����������
        shuffledCharacters = new List<char>(allCharacters);
        ShuffleList(shuffledCharacters);
    }

    // Fisher-Yates ϴ���㷨
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    //
    private IEnumerator SpawnInitialMarbles()
    {
        // ���ѡһ��������Ϊ��ʼ����
        List<string> idioms = new List<string>(idiomDictionary);
        string randomIdiom = idioms[Random.Range(0, idioms.Count)];

        for (int i = 0; i < 20; i++) // ����20����ʼ����
        {
            GameObject newMarbleObj = Instantiate(marblePrefab);
            Marble newMarble = newMarbleObj.GetComponent<Marble>();

            // �����������ѭ��ȡ��
            newMarble.SetCharacter(randomIdiom[i % 4]);

            // ����������ӵ�������ͷ��
            marbleChain.Insert(0, newMarble);

            // ���¼����������ӵ�λ��
            UpdateAllMarblePositions();

            yield return new WaitForSeconds(1f); // ������ɵ�Ч��
        }
    }

    // ���������������е�λ�ã��������ڹ���ϵľ�������
    public void UpdateAllMarblePositions()
    {
        float currentDistance = 0;
        for (int i = 0; i < marbleChain.Count; i++)
        {
            Marble marble = marbleChain[i];
            marble.indexInChain = i;

            // distance / totalLength = a value between 0 and 1
            marble.positionOnPath = pathSpline.Spline.ConvertIndexUnit(currentDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

            // ����GameObject��λ�úͳ���
            Vector3 position = pathSpline.EvaluatePosition(marble.positionOnPath);
            Vector3 forward = (Vector3)pathSpline.EvaluateTangent(marble.positionOnPath);
            marble.transform.position = position;
            marble.transform.up = Vector3.Cross(forward, Vector3.forward); // �����ӳ�����ȷ

            currentDistance += marbleSpacing;
        }
    }

    private void MoveMarbleChain()
    {
        if (marbleChain.Count == 0) return;

        // �������Ӷ���ǰ�ƶ���ͬ�ľ���
        float distanceToMove = marbleSpeed * Time.deltaTime;

        // ������β����ʼ���£�����λ�ü������
        for (int i = marbleChain.Count - 1; i >= 0; i--)
        {
            Marble marble = marbleChain[i];

            // ����ǰλ�ã�0-1�ı�����ת����ʵ�ʾ���
            float currentDistance = pathSpline.Spline.ConvertIndexUnit(marble.positionOnPath, PathIndexUnit.Normalized, PathIndexUnit.Distance);

            // �µľ���
            float newDistance = currentDistance + distanceToMove;

            // ����·��λ��
            marble.positionOnPath = pathSpline.Spline.ConvertIndexUnit(newDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

            // ����GameObject��Transform
            Vector3 position = pathSpline.EvaluatePosition(marble.positionOnPath);
            Vector3 forward = (Vector3)pathSpline.EvaluateTangent(marble.positionOnPath);
            marble.transform.position = position;
            marble.transform.up = Vector3.Cross(forward, Vector3.forward);
        }
    }


    #region �·���
    // ����ָ�����������ӣ�����������ӵ�������
    public IEnumerator SpawnMarbles(int count, float spawnDelay = 0.1f)
    {
        float spawnDistance = 0f; // ��������·�����
        for (int i = 0; i < count; i++)
        {
            if (shuffledCharacters.Count == 0)
                yield break;

            GameObject newMarbleObj = Instantiate(marblePrefab);
            Marble newMarble = newMarbleObj.GetComponent<Marble>();
            newMarble.SetCharacter(shuffledCharacters[0]);
            shuffledCharacters.RemoveAt(0);

            // ���뵽��β
            marbleChain.Add(newMarble);

            // ���ó�ʼλ��
            float normalizedPos = pathSpline.Spline.ConvertIndexUnit(spawnDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            newMarble.positionOnPath = normalizedPos;
            Vector3 position = pathSpline.EvaluatePosition(normalizedPos);
            newMarble.transform.position = position;

            // ��ѡ���𲽲�ֵ��Ŀ��λ��ʵ��˳��
            yield return StartCoroutine(MoveMarbleToPosition(newMarble, normalizedPos, i * marbleSpacing));

            UpdateAllMarblePositions();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // ����˳���ƶ���Ŀ�����
    private IEnumerator MoveMarbleToPosition(Marble marble, float startNorm, float targetDistance)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float startDistance = pathSpline.Spline.ConvertIndexUnit(startNorm, PathIndexUnit.Normalized, PathIndexUnit.Distance);
        float endDistance = startDistance + targetDistance;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curDistance = Mathf.Lerp(startDistance, endDistance, t);
            float norm = pathSpline.Spline.ConvertIndexUnit(curDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
            marble.positionOnPath = norm;
            Vector3 pos = pathSpline.EvaluatePosition(norm);
            Vector3 forward = (Vector3)pathSpline.EvaluateTangent(norm);
            marble.transform.position = pos;
            marble.transform.up = Vector3.Cross(forward, Vector3.forward);
            yield return null;
        }
    }


    #endregion

    // --- ���������߼� ---
    public void CheckForMatches(int insertionIndex)
    {
        // TODO
    }
}