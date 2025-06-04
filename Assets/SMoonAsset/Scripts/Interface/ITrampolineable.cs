public interface ITrampolineable
{
    public int JumpValue();
    public bool IsDamaging();
    public void TrampolineTakeDamage(int damage);
    public void OnTakeTrampoline();
}