public interface ITrampolineable
{
    public int JumpValue();
    public bool IsDamaging();
    public void TakeDamage(int damage);
    public void OnTakeTrampoline();
}