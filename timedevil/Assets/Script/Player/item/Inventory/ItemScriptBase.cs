using UnityEngine;

/* - 아이템 사용시 발동될 스크립트 생성 및 설정 방법
 * 1. 새 스크립트를 생성한다.
 * 2. ItemScriptBase를 상속 받는다.
 * 3. 추상 메소드 Run을 구현한다. 사용시 호출될 메소드임.
 * 4. 새로운 빈 프리팹을 만든다.
 * 5. 그 프리팹에 만든 스크립트를 부착한다.
 * 6. 적용할 ItemSO의 itemScript 필드에 그 프리팹을 부착한다.
*/
public abstract class ItemScriptBase : MonoBehaviour
{
    public abstract void Run(); // 아이템 사용시 실행될 코드
    public abstract bool CanItemUsed(out string msg); // 아이템 사용 가능 여부 판단
}
