using System;
namespace StudentAge.Sanguosha {
 // UI selection is tentative. Only Confirm returns a choice for the rules engine.
 public sealed class TableSelection {
  public Prompt Prompt { get; private set; }
  public int Index { get; private set; } = -1;
  public int Target { get; private set; } = -1;
  public Option Option => Prompt != null && Index >= 0 && Index < Prompt.Options.Count ? Prompt.Options[Index] : null;
  public bool CanConfirm => Option != null && (Option.Target < 0 || Target == Option.Target);
  public void Bind(Prompt prompt) { if (ReferenceEquals(Prompt, prompt)) return; Prompt = prompt; Clear(); }
  public void Clear() { Index = -1; Target = -1; }
  public void Select(int index) { if (Prompt == null || index < 0 || index >= Prompt.Options.Count) return; Index = index; Target = -1; }
  public void SelectTarget(int actor) { if (Option != null && Option.Target == actor) Target = Target == actor ? -1 : actor; }
  public int Confirm(Prompt current) { if (!ReferenceEquals(Prompt,current) || !CanConfirm) { Bind(current); return -1; } int index=Index; Clear(); return index; }
 }
}
