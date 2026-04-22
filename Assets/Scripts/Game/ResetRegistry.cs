using System.Collections.Generic;

// IResettable 오브젝트가 자기 자신을 등록/해제하는 정적 레지스트리
public static class ResetRegistry
{
    private static readonly List<IResettable> _targets = new List<IResettable>();

    public static void Register(IResettable target)
    {
        if (!_targets.Contains(target))
            _targets.Add(target);
    }

    public static void Unregister(IResettable target)
    {
        _targets.Remove(target);
    }

    public static void ResetAll()
    {
        foreach (IResettable target in _targets)
            target.ResetState();
    }
}
