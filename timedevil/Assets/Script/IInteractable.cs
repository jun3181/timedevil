// 이 스크립트는 유니티 오브젝트에 붙이지 않습니다. 그냥 약속(규격)일 뿐입니다.
public interface IInteractable
{
    // IInteractable을 따르는 모든 스크립트는 반드시 Interact() 함수를 가지고 있어야 함.
    void Interact();
}