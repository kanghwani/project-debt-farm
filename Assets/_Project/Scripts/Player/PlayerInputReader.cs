using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")] [Tooltip("유니티 인스펙터에서 Move 연결")] [SerializeField]
    private InputActionReference moveAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference attackAction;

    [Header("Tool Actions")] 
    [Tooltip("숫자 1번 만능손 ")]
    [SerializeField] private InputActionReference tool1Action;
    [Tooltip("숫자 2번 키 씨앗 ")] 
    [SerializeField] private InputActionReference tool2Action;
    
    public Vector2 MoveVector { get; private set; }

    public event Action OnInteractEvent;
    public event Action OnTool1Event;
    public event Action OnTool2Event;
    

    private void Update()
    {
        if (moveAction != null)
        {
            MoveVector = moveAction.action.ReadValue<Vector2>();
        }
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        
        if (interactAction != null) 
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractPerformed;
        }
        if (attackAction != null) attackAction.action.Enable();
        
        if (tool1Action != null)
        {
            tool1Action.action.Enable();
            tool1Action.action.performed += OnTool1Performed;
        }
        if (tool2Action != null)
        {
            tool2Action.action.Enable();
            tool2Action.action.performed += OnTool2Performed;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (interactAction != null) 
        {
            interactAction.action.Disable();
            interactAction.action.performed -= OnInteractPerformed;
        }
        if (attackAction != null) attackAction.action.Disable();
        
        if (tool1Action != null)
        {
            tool1Action.action.Disable();
            tool1Action.action.performed -= OnTool1Performed;
        }
        if (tool2Action != null)
        {
            tool2Action.action.Disable();
            tool2Action.action.performed -= OnTool2Performed;
        }
    }
    private void OnInteractPerformed(InputAction.CallbackContext context) => OnInteractEvent?.Invoke();
    private void OnTool1Performed(InputAction.CallbackContext context) => OnTool1Event?.Invoke();
    private void OnTool2Performed(InputAction.CallbackContext context) => OnTool2Event?.Invoke();
}
