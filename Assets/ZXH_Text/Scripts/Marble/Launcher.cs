using UnityEngine;
using TMPro;

/// <summary>
/// ��ҵķ���������������׼�ͷ��䶯����
/// </summary>
public class Launcher : MonoBehaviour
{
    public GameObject marblePrefab;
    public TMP_Text nextCharText;
    private char nextCharToShoot;

    void Start() 
    { 
        PrepareNextMarble(); 
    }

    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // -90f����Ϊup��Ϊ0��

        if (Input.GetMouseButtonDown(0))
        {
            Shoot(transform.up);
            PrepareNextMarble();
        }
    }

    /// <summary>
    /// ׼����һ��Ҫ����������ַ�����������ʾ�ı�
    /// </summary>
    void PrepareNextMarble()
    {
        nextCharToShoot = GameManager.Instance.GetNextCharForLauncher();
        if (nextCharText != null) nextCharText.text = nextCharToShoot.ToString();
    }

    /// <summary>
    /// ��������
    /// </summary>
    /// <param name="direction">�����ٶ�</param>
    void Shoot(Vector2 direction)
    {
        GameObject shotMarbleObj = Instantiate(marblePrefab, transform.position, Quaternion.identity);
        shotMarbleObj.GetComponent<Marble>().SetCharacter(nextCharToShoot);
        shotMarbleObj.AddComponent<ShotMarble>().Launch(direction);
    }
}