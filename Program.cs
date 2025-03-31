public class OverfillException : Exception
{
    public OverfillException(string message) : base(message) { }
}

public interface IHazardNotifier
{
    void NotifyDanger(string message);
}

public abstract class Container
{
    private static int serialCounter = 1;
    public string SerialNumber { get; private set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public double TareWeight { get; set; }
    public double MaxLoad { get; set; }
    public double CurrentLoad { get; protected set; }

    public Container(double height, double depth, double tareWeight, double maxLoad)
    {
        SerialNumber = GenerateSerialNumber();
        Height = height;
        Depth = depth;
        TareWeight = tareWeight;
        MaxLoad = maxLoad;
        CurrentLoad = 0;
    }

    private string GenerateSerialNumber()
    {
        string typeCode = GetTypeCode();
        return $"KON-{typeCode}-{serialCounter++}";
    }

    protected abstract string GetTypeCode();

    public virtual void Load(double cargoWeight)
    {
        if (cargoWeight < 0) throw new ArgumentException("Waga ładunku nie może być ujemna.");
        if (CurrentLoad + cargoWeight > MaxLoad)
            throw new OverfillException($"Przekroczono maksymalną ładowność kontenera. Maksymalna: {MaxLoad} kg, próbowano załadować: {CurrentLoad + cargoWeight} kg.");

        CurrentLoad += cargoWeight;
        Console.WriteLine($"Załadowano {cargoWeight} kg do kontenera {SerialNumber}. Aktualny ładunek: {CurrentLoad} kg.");
    }

    public virtual void Unload(double cargoWeight)
    {
        if (cargoWeight < 0) throw new ArgumentException("Waga ładunku do rozładowania nie może być ujemna.");
        if (cargoWeight > CurrentLoad) throw new ArgumentException("Nie można rozładować więcej niż aktualny ładunek.");

        CurrentLoad -= cargoWeight;
        Console.WriteLine($"Rozładowano {cargoWeight} kg z kontenera {SerialNumber}. Aktualny ładunek: {CurrentLoad} kg.");
    }

    public void DisplayInfo()
    {
        Console.WriteLine($"Kontener {SerialNumber}: Wysokość={Height}cm, Głębokość={Depth}cm, Waga własna={TareWeight}kg, Maksymalna ładowność={MaxLoad}kg, Aktualny ładunek={CurrentLoad}kg");
    }
}

public class RefrigeratedContainer : Container
{
    public double Temperature { get; set; }
    public string ProductType { get; set; }

    public RefrigeratedContainer(double height, double depth, double tareWeight, double maxLoad, double temperature, string productType)
        : base(height, depth, tareWeight, maxLoad)
    {
        Temperature = temperature;
        ProductType = productType;
    }

    protected override string GetTypeCode() => "C";

    public void ValidateTemperature(double requiredTemperature)
    {
        if (Temperature < requiredTemperature)
            throw new ArgumentException($"Temperatura kontenera ({Temperature}°C) jest za niska dla produktu. Wymagana: {requiredTemperature}°C.");
    }
}

public class LiquidContainer : Container, IHazardNotifier
{
    public bool IsHazardous { get; set; }
    private const double HazardousLimit = 0.5;
    private const double SafeLimit = 0.9;

    public LiquidContainer(double height, double depth, double tareWeight, double maxLoad, bool isHazardous)
        : base(height, depth, tareWeight, maxLoad)
    {
        IsHazardous = isHazardous;
    }

    protected override string GetTypeCode() => "L";

    public override void Load(double cargoWeight)
    {
        double maxAllowed = IsHazardous ? MaxLoad * HazardousLimit : MaxLoad * SafeLimit;
        if (CurrentLoad + cargoWeight > maxAllowed)
            NotifyDanger($"Próba niebezpiecznego załadunku w kontenerze {SerialNumber}. Maksymalna dozwolona waga: {maxAllowed} kg.");

        base.Load(cargoWeight);
    }

    public void NotifyDanger(string message)
    {
        Console.WriteLine($"UWAGA! Niebezpieczna sytuacja w kontenerze {SerialNumber}: {message}");
    }
}

public class GasContainer : Container, IHazardNotifier
{
    public double Pressure { get; set; }
    private const double MinRemainingLoad = 0.05;

    public GasContainer(double height, double depth, double tareWeight, double maxLoad, double pressure)
        : base(height, depth, tareWeight, maxLoad)
    {
        Pressure = pressure;
    }

    protected override string GetTypeCode() => "G";

    public override void Unload(double cargoWeight)
    {
        double minLoad = MaxLoad * MinRemainingLoad;
        if (CurrentLoad - cargoWeight < minLoad)
            throw new ArgumentException($"Nie można rozładować poniżej 5% ładunku. Minimalny wymagany: {minLoad} kg.");

        base.Unload(cargoWeight);
    }

    public void NotifyDanger(string message)
    {
        Console.WriteLine($"UWAGA! Niebezpieczna sytuacja w kontenerze {SerialNumber}: {message}");
    }
}

public class ContainerShip
{
    public string Name { get; set; }
    public double MaxSpeed { get; set; }
    public int MaxContainers { get; set; }
    public double MaxWeight { get; set; }
    private List<Container> Containers { get; set; }

    public ContainerShip(string name, double maxSpeed, int maxContainers, double maxWeight)
    {
        Name = name;
        MaxSpeed = maxSpeed;
        MaxContainers = maxContainers;
        MaxWeight = maxWeight * 1000;
        Containers = new List<Container>();
    }

    public void LoadContainer(Container container)
    {
        if (Containers.Count >= MaxContainers)
            throw new InvalidOperationException($"Przekroczono maksymalną liczbę kontenerów ({MaxContainers}).");

        double totalWeight = Containers.Sum(c => c.TareWeight + c.CurrentLoad) + container.TareWeight + container.CurrentLoad;
        if (totalWeight > MaxWeight)
            throw new OverfillException($"Przekroczono maksymalną wagę statku ({MaxWeight} kg).");

        Containers.Add(container);
        Console.WriteLine($"Załadowano kontener {container.SerialNumber} na statek {Name}.");
    }

    public void RemoveContainer(string serialNumber)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container == null) throw new ArgumentException($"Kontener o numerze {serialNumber} nie znaleziony.");

        Containers.Remove(container);
        Console.WriteLine($"Usunięto kontener {serialNumber} ze statku {Name}.");
    }

    public void DisplayShipInfo()
    {
        Console.WriteLine($"Statek {Name}: Prędkość max={MaxSpeed} węzłów, Maks. kontenerów={MaxContainers}, Maks. waga={MaxWeight/1000} ton");
        Console.WriteLine("Lista kontenerów:");
        if (Containers.Count == 0) Console.WriteLine("Brak");
        else foreach (var container in Containers) container.DisplayInfo();
    }

    public void TransferContainer(string serialNumber, ContainerShip targetShip)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container == null) throw new ArgumentException($"Kontener o numerze {serialNumber} nie znaleziony.");

        RemoveContainer(serialNumber);
        targetShip.LoadContainer(container);
        Console.WriteLine($"Przeniesiono kontener {serialNumber} ze statku {Name} na statek {targetShip.Name}.");
    }
}

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var ship = new ContainerShip("Statek 1", 10, 100, 40000);

            var bananaContainer = new RefrigeratedContainer(200, 200, 500, 20000, 13.3, "Bananas");
            var fuelContainer = new LiquidContainer(200, 200, 600, 15000, true);
            var gasContainer = new GasContainer(200, 200, 700, 10000, 10);

            bananaContainer.Load(15000);
            fuelContainer.Load(7500);
            gasContainer.Load(9000);

            ship.LoadContainer(bananaContainer);
            ship.LoadContainer(fuelContainer);
            ship.LoadContainer(gasContainer);

            ship.DisplayShipInfo();

            try
            {
                fuelContainer.Load(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
            }

            gasContainer.Unload(8550);

            gasContainer.DisplayInfo();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }
}