## Java Coding Convention

| 항목 | 규칙 |
| --- | --- |
| 중괄호 `{` | 선언문과 같은 줄 |
| AI 사용 코드 | 위아래에 주석으로 범위표시
`// AI-assisted code start
// AI-assisted code end` |

## Naming Convention

| 종류 | 예시 |
| --- | --- |
| 클래스 | `ExampleClass` |
| 인터페이스 | `ExampleInterface`  |
| 메서드 | `exampleMethod()` |
| 변수 | `exampleVariable` |
| 매개변수 | `exampleParameter` |
| 상수 | `EXAMPLE_CONSTANT` |
| 패키지 | `example.package` |

## Class-Internal Convention

```
public class ExampleClass {
		// 1. 상수
		private static final int EXAMPLE_CONSTANT = 5;
		
		// 2. 필드
		private final ExampleField exampleField;
		
		// 3. 생성자
		public ExampleClass(ExampleField exampleField) {
		    this.exampleField = exampleField;
		}
		
		// 4. 메서드 (public -> private 순서)
		public User exampleMethod(long exampleParameter) {
	
		}
}
```

## Additional Convention

- 기본적으로 필드는  private 작성 (필요시 getter, setter 제공)
- 숫자나 문자열 대신 상수 사용 권장
- 메서드 작성시 이름은 동사로 시작 (create, find, delete, update 등)
- 중요 메서드는 javadoc 주석 사용 권장

## Development Setup

| 항목 | 버전 |
| --- | --- |
| Java | **21 LTS** |
| JavaFX | **21.0.6** |
| JUnit | **5.11.4** |
| Build | Maven |
| IDE | VS Code |