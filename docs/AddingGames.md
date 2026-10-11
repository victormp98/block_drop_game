# Añadir un juego a BlockDrop Pocket

Cada juego tiene su carpeta dentro de `Games`. Sus reglas y su dibujo pertenecen a esa carpeta; la carcasa, los controles, el menú y el guardado compartido ya están implementados.

## 1. Crear la carpeta y el módulo

Los módulos existentes (`Tetris`, `Snake`, `Racing` y `Platformer`) sirven de referencia. Para un juego adicional, por ejemplo un rompecabezas:

```text
Games/Puzzle/
├── PuzzleGame.cs     # reglas y puntuación
├── PuzzleScreen.cs   # dibujo de la partida
└── PuzzleModule.cs   # implementación de IRetroGame
```

Usa un espacio de nombres propio, como `BlockDrop.Games.Puzzle`. `Games/Tetris/TetrisModule.cs` es un ejemplo completo del patrón; `PlatformerModule` muestra cómo manejar controles mantenidos.

## 2. Implementar IRetroGame

El contrato está en `Shared/Contracts/IRetroGame.cs`:

| Miembro | Responsabilidad del juego |
| --- | --- |
| `Definition` | ID, título, leyenda de pantalla, acciones A/B y ayuda |
| `Started`, `Paused`, `GameOver`, `Score` | Exponer el estado de la partida |
| `Won` | Indicar victoria al terminar; por defecto es `false` |
| `Start()` | Iniciar una partida y reiniciar su estado |
| `SetPaused(bool)` | Detener o reanudar la partida |
| `Update(double seconds)` | Avanzar las reglas con el tiempo recibido |
| `HandleInput(GameButton)` | Interpretar arriba, abajo, izquierda, derecha, A y B |
| `ReleaseInput(GameButton)` | Dejar de moverse al soltar una dirección; opcional para controles discretos |
| `Draw(ICanvas, RectF, GameDrawContext)` | Dibujar la partida dentro del LCD |

START, SELECT, la navegación del menú y el regreso desde ayuda o récords los administra la consola. Cada juego decide qué significan A y B durante la partida. Una partida pausada o terminada debe ignorar los movimientos y el avance del tiempo. Si mantiene direcciones, debe limpiarlas al pausar para evitar que sigan activas al cambiar de juego. Para terminar con victoria, publica `GameOver = true` y `Won = true`; la consola muestra el aviso y guarda la puntuación.

El dibujo recibe coordenadas lógicas del LCD, con origen `(0, 0)` y tamaño indicado por `bounds`. La consola ya aplica la escala para el dispositivo. El juego debe mantenerse dentro de esos límites. `GameDrawContext.Best` contiene su récord.

Puedes reutilizar `Shared/Graphics/LcdGraphics.cs` para colores, letras y bloques, y `PixelArt.cs` para sprites recortados dentro del área de juego. Mantén los recursos específicos dentro de su módulo; si son imágenes o sonidos que deben empaquetarse con MAUI, usa una subcarpeta propia en `Resources/Images` o `Resources/Raw` y declárala en el proyecto.

## 3. Registrar el módulo

Después de implementar el nuevo módulo, añade una instancia al registro existente en `Console/GameLibrary.cs`:

```csharp
using BlockDrop.Games.Snake;
using BlockDrop.Games.Tetris;
using BlockDrop.Games.Racing;
using BlockDrop.Games.Platformer;
using BlockDrop.Games.Puzzle;

public static GameCatalog Create() => new([
    new TetrisModule(),
    new SnakeModule(),
    new RacingModule(),
    new PlatformerModule(),
    new PuzzleModule()
]);
```

El menú reconoce los juegos registrados y usa su título. No necesitas añadir reglas del juego nuevo a `ConsolePage`, `ConsoleController` o `ConsoleScreen`.

El ID debe ser único, estable y usar minúsculas, números o guiones, por ejemplo `puzzle`. Cambiarlo crea una carpeta de récords diferente. La consola guarda sus puntuaciones en `games/puzzle/scores.json` y conserva cada partida en su propia sesión mientras la aplicación permanece abierta.

`LegacyScoreFile` solo se usa para migrar una versión anterior. Déjalo sin asignar en los nuevos juegos.

## 4. Comprobar la integración

Añade las pruebas de sus reglas a una carpeta propia, por ejemplo `Tests/Puzzle`, y referencia esos archivos en el proyecto de comprobaciones y en `Tests/Program.cs`. Los proyectos ya incluyen automáticamente los módulos de `Games`. Añade el módulo nuevo a las comprobaciones de pantalla y actualiza la prueba del catálogo. Verifica inicio, controles, límites, colisiones, puntuación, pausa y reinicio. Mantén los módulos simulados dentro de `Tests`.

```powershell
dotnet run --project Tests/BlockDrop.GameChecks.csproj
dotnet run --project Tests/BlockDrop.ScreenChecks.csproj
dotnet build BlockDrop.csproj -f net10.0-windows10.0.19041.0
dotnet build BlockDrop.csproj -f net10.0-android
```
