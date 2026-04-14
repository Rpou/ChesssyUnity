public class EvalMove
{
    public int value { get; }
    public Move? move { get; }

    public EvalMove(int value, Move? move)
    {
        this.value = value;
        this.move = move;
    }
}