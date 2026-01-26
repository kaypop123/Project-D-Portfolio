// PlayerSaveAgent.cs
using System;
using UnityEngine;

[Serializable]
public class PlayerSaveDTO
{
    public float x, y;
    public float lastDirection; // 네 PlayerMovement에서 쓰는 방향(확장 대비)
}

[RequireComponent(typeof(SaveId))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerSaveAgent : MonoBehaviour, ISaveable
{
    SaveId saveId;
    Rigidbody2D rb;
    PlayerMovement movement;

    void Awake()
    {
        saveId = GetComponent<SaveId>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
    }

    public string GetSaveId() => saveId.Guid;

    public object CaptureState()
    {
        return new PlayerSaveDTO
        {
            x = rb.position.x,
            y = rb.position.y,
            lastDirection = movement != null ? movement.LastDirection : 1f
        };
    }

    public void RestoreState(object state)
    {
        if (state is PlayerSaveDTO dto)
        {
            // 물리 안정 위해 velocity 초기화 후 위치 세팅
            rb.linearVelocity = Vector2.zero;
            rb.position = new Vector2(dto.x, dto.y);

            // 방향 복원(필요 시 애니/플립에 반영하는 로직 추가)
            // 예: movement.SetFacing(dto.lastDirection);
        }
    }
}
