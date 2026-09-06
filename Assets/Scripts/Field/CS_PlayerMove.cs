using UnityEngine;

/// <summary>
/// フィールドでのプレイヤー移動。Rigidbody2Dで上下左右に自由移動する
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CS_PlayerMove : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 3f;

    private Rigidbody2D _rigidbody;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        _moveInput = new Vector2(horizontal, vertical).normalized;
    }

    private void FixedUpdate()
    {
        _rigidbody.MovePosition(_rigidbody.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }
}
