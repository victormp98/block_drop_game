# BlockDrop Pocket

Consola retro para .NET MAUI con carcasa gris, botones negros y pantalla LCD monocromática. Al abrir la aplicación aparece el menú dentro de la misma pantalla del juego. Los controles permanecen visibles.

## Juegos incluidos

| Juego | Qué incluye | Cruceta | A | B |
| --- | --- | --- | --- | --- |
| Tetris | Siete piezas, siguiente pieza, guía de caída, filas y niveles | Mover, bajar; arriba gira | Girar | Soltar |
| Gusanito | Comida, crecimiento, velocidad progresiva y colisiones con paredes y cuerpo | Elegir dirección | Girar a la derecha | Girar a la izquierda |
| Carritos | Tres carriles, tráfico, tres vidas, dificultad progresiva y combustible | Cambiar de carril; arriba acelera, abajo frena | Turbo | Frenar |
| Pocket Run | Aventura original con tres niveles, saltos, monedas, enemigos y puntos de control | Mantener izquierda/derecha para caminar; arriba salta | Saltar | Alternar correr/caminar |

START confirma, pausa o continúa. SELECT abre el menú y conserva la partida pausada. En el menú usa la cruceta para elegir, A para aceptar y B para volver. Puedes tocar las opciones visibles del LCD. El menú se desplaza para mostrar todas las opciones.

En Windows también puedes usar las flechas, A, B o espacio para la acción B, Enter o P para START y Escape para SELECT. Pocket Run permite mantener una dirección mientras saltas con A o arriba; al soltar la dirección se detiene.

Cada juego conserva su propia sesión mientras la app está abierta y sus cinco mejores puntuaciones en el dispositivo. Cambiar de juego pausa el anterior; seleccionarlo nuevamente permite continuar. Pocket Run termina con una pantalla de victoria al completar los tres niveles. La app se pausa al cambiar de aplicación. Android usa orientación vertical y oculta las barras del sistema; puedes mostrarlas deslizando desde el borde.

## Organización

La consola y los juegos tienen responsabilidades separadas:

```text
BlockDrop/
├── Console/
│   ├── ConsolePage.xaml       # carcasa y controles físicos
│   ├── ConsolePage.xaml.cs    # conexión con MAUI, teclado y temporizador
│   ├── ConsoleController.cs   # menú, selección, pausa y sesiones
│   ├── ConsoleScreen.cs       # dibujo del menú, ayuda y récords
│   ├── ConsoleViewState.cs    # estado de la pantalla y opciones del menú
│   ├── GameCatalog.cs         # catálogo de juegos
│   └── GameLibrary.cs         # registro de los cuatro módulos
├── Games/
│   ├── Tetris/               # TetrisGame, TetrisScreen y TetrisModule
│   ├── Snake/                # SnakeGame, SnakeScreen y SnakeModule
│   ├── Racing/               # RacingGame, RacingScreen y RacingModule
│   └── Platformer/           # PlatformerGame, Level, Screen y Module
├── Shared/
│   ├── Contracts/            # instrucciones comunes para los juegos
│   ├── Graphics/             # paleta, letras y dibujo de píxeles
│   └── Storage/              # guardado de récords por juego
├── Tests/
│   ├── Tetris/               # comprobaciones de cada juego
│   ├── Snake/
│   ├── Racing/
│   ├── Platformer/
│   ├── Console/              # navegación, sesiones y pantalla
│   └── Shared/               # guardado y migración
└── docs/
    └── AddingGames.md        # cómo añadir otro juego
```

Cada juego implementa `IRetroGame`. La consola le envía botones, liberaciones y tiempo transcurrido, y le pide dibujar la partida. Las reglas y los gráficos específicos están en su carpeta. Los sprites originales se dibujan con píxeles usando la misma paleta LCD.

`GameLibrary.cs` es el punto donde se registran los módulos. El menú se genera a partir del catálogo y permite desplazarse cuando hay más opciones. Cada módulo aporta sus propias etiquetas para A y B y sus instrucciones. Al cambiar de juego, la sesión anterior queda pausada y se puede continuar.

El catálogo incluye Tetris, Gusanito, Carritos y Pocket Run. Sus IDs son `tetris`, `snake`, `racing` y `platformer`. La guía para añadir otro módulo está en [docs/AddingGames.md](docs/AddingGames.md).

Los récords se guardan en `games/<id-del-juego>/scores.json` dentro de los datos de la aplicación. Los récords del Tetris de la versión anterior se copian automáticamente a `games/tetris/scores.json`, conservando el archivo original. En Windows puedes indicar una carpeta de datos alternativa con `BLOCKDROP_DATA_DIRECTORY`.

## Compilar

Requiere .NET 10 y la carga de trabajo MAUI correspondiente.

```powershell
dotnet build BlockDrop.csproj -f net10.0-windows10.0.19041.0
dotnet build BlockDrop.csproj -f net10.0-android
```

La aplicación Windows se genera en `bin/Debug/net10.0-windows10.0.19041.0/win-x64/BlockDrop.exe`. El APK firmado de desarrollo está en `bin/Debug/net10.0-android/com.victormp98.blockdrop-Signed.apk`.

El APK incluye los ensamblados del juego para instalarlo directamente sin depender de Visual Studio. La copia de entrega, versión 1.1, está en `artifacts/BlockDrop-Pocket.apk`.

## Comprobar juegos y consola

```powershell
dotnet run --project Tests/BlockDrop.GameChecks.csproj
dotnet run --project Tests/BlockDrop.ScreenChecks.csproj
```

Las comprobaciones verifican reglas de los cuatro juegos, colisiones, puntuación, pausa, reinicio, liberación de botones, victoria y una partida que completa los tres niveles originales usando los controles normales. También comprueban la separación de partidas y récords, migración de datos, navegación y dibujo LCD en cinco tamaños por juego. Los módulos auxiliares de prueba no se incluyen en la aplicación.
