using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Rigidbody2D))]
public class Trolley : MonoBehaviour
{
    public Rigidbody2D rb { get; private set; }

    [Header("Trolley Stats")] 
    public float currentWeight = 0f;
    public float baseMass = 1f;
    public float baseDrag = 4f;

    [Header("Drive Settings")] public float driveForce = 500f;
    
    public Stack<StackedCrop> cropStack { get; private set; } = new Stack<StackedCrop>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 물리
        rb.gravityScale = 0f;
        rb.mass = baseMass;
        rb.linearDamping = baseDrag;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    
    public void PushCrop(StackedCrop crop)
    {
        // 이미 포장된 구조체를 그대로 스택에 넣습니다.
        cropStack.Push(crop);
        currentWeight += crop.weight;
        UpdatePhysicsModifiers();
        
        Debug.Log($" {crop.data.cropName} 적재! (등급: {crop.grade}, 무게: {crop.weight}kg)");
    }
    
    
    // 상자 납품용 함수 
    public StackedCrop? PopCrop()
    {
        if (cropStack.Count == 0) return null;

        StackedCrop poppedCrop = cropStack.Pop();
        currentWeight -= poppedCrop.weight;
    
        if (currentWeight < 0) currentWeight = 0; 
        UpdatePhysicsModifiers();

        //
        Debug.Log($" 수레에서 꺼냄: {poppedCrop.data.cropName} / 남은 개수: {cropStack.Count}");
        return poppedCrop;
    }

    private void UpdatePhysicsModifiers()
    {
        rb.mass = baseMass + (currentWeight * 0.2f);
        rb.linearDamping = baseDrag + (currentWeight * 0.1f);
    }
    
    public void Drive(Vector2 inputDirection)
    {
        rb.AddForce(inputDirection * driveForce * Time.fixedDeltaTime, ForceMode2D.Force);
    }
    









}
