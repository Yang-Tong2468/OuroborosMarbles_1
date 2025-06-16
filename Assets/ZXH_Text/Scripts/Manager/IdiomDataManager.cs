// IdiomDataManager.cs
// ְ��ר�Ź���������ݣ��������ء�ɸѡ���ַ���ʵ����������Ϸ�߼��ķ��롣

using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ���ڸ�Ч���������

public class IdiomDataManager
{
    // [��һ������]: ��Ϸ��֪�����г�����ֿܲ�
    private HashSet<string> _fullIdiomDictionary;

    // [�ڶ�������]: Ϊ������Ϸ�ر���ѡ����һ�����
    private List<string> _sessionIdioms;

    // [����������]: �����ֳ����ֳɵ����е�������
    private List<char> _sessionCharacterPool;

    // [���ļ�����]: ����˳���ĺ��ֶ��У�������ҷ���
    private Queue<char> _shuffledCharacterQueue;

    // [�����ݽṹ] ר���������ɳ�ʼ�������Ķ���
    private Queue<char> _initialSpawnQueue;

    private List<char> _initialSpawnBackup;   // ��ʼ���ɶ��еı���
    private List<char> _shootBackup;          // ������еı���

    /// <summary>
    /// ���캯��������Ϸ��ʼʱ��ʼ���������ݹܵ���
    /// </summary>
    /// <param name="idiomFile">�������г�����ı��ļ���Դ</param>
    /// <param name="idiomsForThisSession">Ϊ������Ϸ��ѡ���ٸ�����</param>
    public IdiomDataManager(TextAsset idiomFile, int idiomsForThisSession)
    {
        // ����1: �������г��ﵽ�ֿܲ�
        _fullIdiomDictionary = new HashSet<string>();
        LoadAllIdiomsFromFile(idiomFile);

        // ����2: ���ֿܲ���Ϊ������Ϸ��ѡ����
        _sessionIdioms = new List<string>();
        SelectIdiomsForSession(idiomsForThisSession);

        // ����3: ����ѡ�еĳ���������ֵġ��ֳء�
        _sessionCharacterPool = new List<char>();
        PopulateCharacterPool();

        // ����4: ���ֳش��ң��γɷ������
        _shuffledCharacterQueue = new Queue<char>();
        _initialSpawnQueue = new Queue<char>();
        ShuffleAndDistributePools();
    }

    private void LoadAllIdiomsFromFile(TextAsset file)
    {
        if (file == null)
        {
            Debug.LogError("����ʵ��ļ�δ�ṩ��");
            return;
        }
        string[] lines = file.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (line.Trim().Length == 4)
            {
                _fullIdiomDictionary.Add(line.Trim());
            }
        }
    }

    private void SelectIdiomsForSession(int count)
    {
        List<string> allIdiomsList = _fullIdiomDictionary.ToList();
        // ���������������ڵ�����������ʹ�����г���
        if (allIdiomsList.Count <= count)
        {
            _sessionIdioms = allIdiomsList;
            return;
        }

        // �����ѡ���ظ��ĳ���
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, allIdiomsList.Count);
            _sessionIdioms.Add(allIdiomsList[randomIndex]);
            allIdiomsList.RemoveAt(randomIndex);
        }

        foreach(var chengyu in _sessionIdioms)
        {
            Debug.Log($"���ֳ��{chengyu}/n");
        }
    }

    private void PopulateCharacterPool()
    {
        foreach (string idiom in _sessionIdioms)
        {
            _sessionCharacterPool.AddRange(idiom.ToCharArray());
        }
    }

    /// <summary>
    /// [�·���] ���ֳش��ң�����һ���������������ʼ���ɶ��С��͡�������С�
    /// </summary>
    private void ShuffleAndDistributePools()
    {
        var shuffledList = _sessionCharacterPool.OrderBy(x => System.Guid.NewGuid()).ToList();

        // ���磬���ǿ��԰�ǰһ��������ڳ�ʼ���ɣ���һ�����ڷ���
        // ����������Ը���������������
        int splitIndex = Mathf.CeilToInt(shuffledList.Count / 2f);

        // ����
        _initialSpawnBackup = shuffledList.Take(splitIndex).ToList();
        // ���ݣ�ȫ���ֶ����ݵ�_shootBackup
        _shootBackup = new List<char>(shuffledList);

        // ����ʼ���ɶ���
        for (int i = 0; i < splitIndex; i++)
        {
            _initialSpawnQueue.Enqueue(shuffledList[i]);
        }

        // ��䷢����У�ȫ����
        _shuffledCharacterQueue = new Queue<char>(_shootBackup);

        Debug.Log($"���ݳ�ʼ������ʼ���ɶ��� {_initialSpawnQueue.Count} ���֣�������� {_shuffledCharacterQueue.Count} ����");
    }

    /// <summary>
    /// �����ֳز���䷢�����
    /// </summary>
    private void ShuffleAndFillQueue()
    {
        // ʹ�� Linq �� OrderBy �� Guid ʵ��һ����Ч���������
        var shuffledList = _sessionCharacterPool.OrderBy(x => System.Guid.NewGuid()).ToList();
        _shuffledCharacterQueue = new Queue<char>(shuffledList);
    }


    /// <summary>
    /// ���һ���ַ����Ƿ��Ǳ�����Ϸ�е���Ч����
    /// </summary>
    public bool IsValidSessionIdiom(string potentialIdiom)
    {
        // ֻ��С��Χ�ı��ֳ����в��ң����ܸ�
        return _sessionIdioms.Contains(potentialIdiom);
    }

    /// <summary>
    /// ��ȡһ������ĳ���������ɳ�ʼ��������
    /// </summary>
    public string GetRandomSessionIdiom()
    {
        if (_sessionIdioms.Count == 0) return "��������"; // ���ó���
        return _sessionIdioms[Random.Range(0, _sessionIdioms.Count)];
    }

    /// <summary>
    /// �ӳ�ʼ���ɶ�����ȡ��һ���֡�
    /// </summary>
    public char GetNextCharacterForInitialSpawn()
    {
        if (_initialSpawnQueue.Count > 0)
        {
            return _initialSpawnQueue.Dequeue();
        }

        // �ñ������´��Ҳ���
        Debug.LogWarning("��ʼ���ɶ����ѿգ����´��Ҳ��䣡");
        var reshuffled = _initialSpawnBackup.OrderBy(x => System.Guid.NewGuid()).ToList();
        _initialSpawnQueue = new Queue<char>(reshuffled);
        return _initialSpawnQueue.Dequeue();
    }

    /// <summary>
    /// �ӷ��������ȡ��һ���֣��������Ϊ�����Զ��������
    /// </summary>
    public char GetNextCharacterToShoot()
    {
        if (_shuffledCharacterQueue.Count == 0)
        {
            Debug.LogWarning("��������ѿգ����´��Ҳ��䣡");
            var reshuffled = _shootBackup.OrderBy(x => System.Guid.NewGuid()).ToList();
            _shuffledCharacterQueue = new Queue<char>(reshuffled);
        }

        // Queue.Dequeue() ���Զ�ȡ�����Ƴ���һ��Ԫ��
        return _shuffledCharacterQueue.Dequeue();
    }
}