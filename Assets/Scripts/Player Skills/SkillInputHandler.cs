using UnityEngine;
using UnityEngine.InputSystem;

public class SkillInputHandler : MonoBehaviour
{
    public CooldownSkillUI skillQ;
    public CooldownSkillUI skillE;

    private CustomActions inputActions;

    private void Awake()
    {
        inputActions = new CustomActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Main.SpellCast.performed += OnQPressed;
        inputActions.Main.Teleport.performed += OnEPressed;
    }

    private void OnDisable()
    {
        inputActions.Main.SpellCast.performed -= OnQPressed;
        inputActions.Main.Teleport.performed -= OnEPressed;
        inputActions.Disable();
    }

    private void OnQPressed(InputAction.CallbackContext context)
    {
        if (!skillQ.IsOnCooldown())
        {
            skillQ.TriggerCooldown();
            Debug.Log("Skill Q (SpellCast) usada!");
        }
    }

    private void OnEPressed(InputAction.CallbackContext context)
    {
        if (!skillE.IsOnCooldown())
        {
            skillE.TriggerCooldown();
            Debug.Log("Skill E (Teleport) usada!");
        }
    }
}