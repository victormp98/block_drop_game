namespace BlockDrop.Games.Racing;

public sealed class TrafficCar(int lane, double position)
{
    public int Lane { get; } = lane;
    public double Position { get; internal set; } = position;
}

public sealed class RacingGame
{
    public const double PlayerPosition = 86;
    private readonly Random random;
    private readonly List<TrafficCar> traffic = [];
    private readonly IReadOnlyList<TrafficCar> trafficView;
    private double spawnDistance;
    private double effectTime;
    private int speedMode;
    private double invincible;
    private int passed;
    public IReadOnlyList<TrafficCar> Traffic => trafficView;
    public bool Started { get; private set; }
    public bool Paused { get; private set; }
    public bool GameOver { get; private set; }
    public int Lane { get; private set; }
    public int Lives { get; private set; }
    public double Distance { get; private set; }
    public int Level => 1 + (int)(Distance / 350);
    public int Score => (int)(Distance * 2) + passed * 50;
    public bool Turbo => speedMode == 1;
    public bool Braking => speedMode == -1;
    public double Invincible => invincible;
    public double Speed => (34 + Math.Min(26, (Level - 1) * 3)) * (Turbo ? 1.65 : Braking ? 0.55 : 1);
    public double Fuel { get; private set; }

    public RacingGame(Random? random = null)
    {
        this.random = random ?? new Random();
        trafficView = traffic.AsReadOnly();
    }

    public void Start()
    {
        Started = true;
        Paused = GameOver = false;
        Lane = 1;
        Lives = 3;
        Fuel = 100;
        Distance = spawnDistance = effectTime = invincible = 0;
        speedMode = passed = 0;
        traffic.Clear();
        SpawnWave();
    }

    public void SetPaused(bool paused) => Paused = paused;
    public void Move(int direction)
    {
        if (Started && !Paused && !GameOver) Lane = Math.Clamp(Lane + Math.Sign(direction), 0, 2);
    }
    public void Accelerate()
    {
        if (!Started || Paused || GameOver || Fuel < 20) return;
        speedMode = 1;
        effectTime = 1.5;
    }
    public void Brake()
    {
        if (!Started || Paused || GameOver) return;
        speedMode = -1;
        effectTime = 1.2;
    }

    public void Tick(double seconds)
    {
        if (!Started || Paused || GameOver || !double.IsFinite(seconds) || seconds <= 0) return;
        var remaining = Math.Min(seconds, 0.25);
        while (remaining > 0 && !GameOver)
        {
            var dt = Math.Min(remaining, 1.0 / 120);
            remaining -= dt;
            Step(dt);
        }
    }

    private void Step(double dt)
    {
        invincible = Math.Max(0, invincible - dt);
        effectTime -= dt;
        if (effectTime <= 0 || (Turbo && Fuel <= 0)) speedMode = 0;
        Fuel = Math.Clamp(Fuel + (Turbo ? -45 : 12) * dt, 0, 100);
        var advance = Speed * dt;
        Distance += advance;
        spawnDistance += advance;
        foreach (var car in traffic) car.Position += advance;
        // Every wave has an open lane; distance between waves remains large enough to react.
        if (spawnDistance >= 38) { spawnDistance -= 38; SpawnWave(); }
        foreach (var car in traffic.ToArray())
        {
            if (car.Lane == Lane && Math.Abs(car.Position - PlayerPosition) < 11 && invincible <= 0)
            {
                traffic.Remove(car);
                Lives--;
                invincible = 1.5;
                speedMode = 0;
                if (Lives == 0) { GameOver = true; break; }
            }
            else if (car.Position > 112) { traffic.Remove(car); passed++; }
        }
    }

    private void SpawnWave()
    {
        var first = random.Next(3);
        traffic.Add(new TrafficCar(first, -12));
        if (Level >= 3 && random.Next(3) == 0) traffic.Add(new TrafficCar((first + 1 + random.Next(2)) % 3, -12));
    }
}
