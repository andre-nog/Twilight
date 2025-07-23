using UnityEngine;
using UnityEngine.InputSystem;

public class SkillInputHandler : MonoBehaviour
{
    public CooldownSkillUI skillE; // Apenas o E continua controlado aqui (opcional)

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
        // Fireball (Q) é tratada pelo PlayerMagicSystem (com ou sem smartcast)
        Debug.Log("Skill Q pressionada – delegando execução ao PlayerMagicSystem.");
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