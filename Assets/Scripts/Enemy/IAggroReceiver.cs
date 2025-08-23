/// <summary>
/// Interface para receptores de aggro (inimigos, torres, etc.).
/// </summary>
public interface IAggroReceiver
{
    /// <summary>
    /// Invocado para fazer este objeto ganhar aggro em um alvo.
    /// </summary>
    void TakeAggro();
}
