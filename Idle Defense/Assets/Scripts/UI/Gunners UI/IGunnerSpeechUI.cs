public interface IGunnerSpeechUI
{
    // listener can be null for one-liners
    void ShowLine(GunnerSO speaker, GunnerSO listener, string text);
}
