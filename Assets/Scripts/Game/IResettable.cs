// 게임 리셋 대상 오브젝트가 구현하는 인터페이스
// ResetRegistry에 자동 등록되어 GameSessionReset에서 일괄 호출됨
public interface IResettable
{
    void ResetState();
}
