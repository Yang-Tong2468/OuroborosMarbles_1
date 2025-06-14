using UnityEngine;
using TMPro; // ���� TextMeshPro �����ռ�

public class Marble : MonoBehaviour
{
    public TMP_Text characterText; // ��ק��������Ӷ�������
    public int indexInChain;       // ���������е�����
    public float positionOnPath;   // ��·���ϵ�λ�� (0.0 to 1.0)

    private char _character;

    // ����������ʾ������
    public void SetCharacter(char c)
    {
        _character = c;
        characterText.text = c.ToString();
    }

    public char GetCharacter()
    {
        return _character;
    }
}