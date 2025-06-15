// Marble.cs
// ְ�𣺴��������ӣ���������ۣ����֡����������������ԣ�ֱ������

using UnityEngine;
using TMPro;

public class Marble : MonoBehaviour
{
    // [�ؼ�����] ���������ϵ� TextMeshPro ���
    public TMP_Text characterText;

    // [��������] ��¼�������ڹ���ϵľ�ȷ·�̾��롣�� GameManager ���¡�
    public float distanceOnPath;

    // [˽�б���]
    private char _character;
    private SpriteRenderer _spriteRenderer;
    private float _currentRotationZ = 0f; // �ۼ���ת�Ƕȣ���ʵ����������
    private float _currentZRotation = 0f;

    // [��������] ���ⲿ�ṩ���ӵ�ֱ�������ھ�ȷ����ײ�ͼ�����
    public float Diameter => _spriteRenderer != null ? _spriteRenderer.bounds.size.x : 1f;

    void Awake()
    {
        // �Զ���ȡ SpriteRenderer �����Ϊ����ֱ����׼��
        // ����ʹ�� GetComponentInChildren ��Ϊ�˼��ݰ� Sprite ��Ϊ�Ӷ�������
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Debug.LogError("Marble Prefab �������һ�� SpriteRenderer �����", this);
        }
    }

    /// <summary>
    /// ����������ʾ�ĺ��֡�
    /// </summary>
    public void SetCharacter(char c)
    {
        _character = c;
        if (characterText != null)
        {
            characterText.text = c.ToString();
        }
    }

    /// <summary>
    /// ��ȡ���Ӵ���ĺ��֡�
    /// </summary>
    public char GetCharacter()
    {
        return _character;
    }

    ///// <summary>
    ///// [�����Ӿ��߼� V2.0] �����ƶ��ķ���;������������ӵĹ���Ч����
    ///// </summary>
    ///// <param name="movementVector">��һ֡�����ƶ������� (��λ�� - ��λ��)</param>
    //public void UpdateRotation(Vector2 velocity)
    //{
    //    // �ƶ����� = �ٶȴ�С * ʱ��
    //    float distanceMoved = velocity.magnitude * Time.deltaTime;
    //    if (distanceMoved <= 0.0001f || Diameter <= 0) return;

    //    // 1. ������ת�Ƕ� (�߼�����)
    //    float circumference = Mathf.PI * Diameter;
    //    float degreesToRotate = (distanceMoved / circumference) * 360f;

    //    // 2. ������ת�� (�߼�����)
    //    Vector3 rotationAxis = Vector3.Cross(velocity.normalized, Vector3.forward);

    //    // 3. Ӧ����ת (�߼�����)
    //    Quaternion deltaRotation = Quaternion.AngleAxis(degreesToRotate, rotationAxis);
    //    transform.rotation = deltaRotation * transform.rotation;
    //}

    /// <summary>
    /// ����һ֡���ƶ��ľ��룬�������ӵĹ���Ч������Z����ת����
    /// </summary>
    /// <param name="distanceMoved">��һ֡�����ƶ��ľ��롣</param>
    public void UpdateRotation(float distanceMoved)
    {
        if (distanceMoved <= 0.0001f || Diameter <= 0) return;
        float circumference = Mathf.PI * Diameter;
        float degreesToRotate = (distanceMoved / circumference) * 360f;
        _currentZRotation -= degreesToRotate;
        transform.rotation = Quaternion.Euler(0f, 0f, _currentZRotation);
    }
}