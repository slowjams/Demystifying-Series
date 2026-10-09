## Demystifying Metrics

Summary Table
Object	                     
`Counter<int>`	           Instrument
`Counter<int>.Add`	       Measurement is recorded
`MetricState`	           Routes the measurement to the correct Metric
`Metric`	               Represents an **aggregated, export-ready view** of a metric stream
`AggregatorStore`	       Finds or creates a MetricPoint for the tag set
`MetricPoint`	           Updates the aggregated value (e.g., increments the sum)

note that instrument itself is stateless, it doesn't contain an internal "counter" to "record" measurement as you will natually thing a Counter must have a state that "increment" everytime when we do `counter.Add(10)`.

## Prerequsite

**How `MeterListener` interacts with `Instrument` on `ListenerSubscription`**

```C#
//------------------------------------V
class Program
{
    static void Main()
    {
        var meter = new Meter("DemoApp");  // <---------------add itself to Meter's static `s_allMeters`
        var counter = meter.CreateCounter<int>("my_counter");

        // First MeterListener
        var listener1 = new MeterListener();

        listener1.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>
        {
            Console.WriteLine($"Listener1 received: {value}");
        });
        
        listener1.InstrumentPublished = (instrument, listener) =>  // <----------------lip
        {
            if (instrument.Name == "my_counter")
                listener.EnableMeasurementEvents(instrument, "Listener1");  // <-------add new ListenerSubscription(listener1, "Listener1") to instrument's _subscriptions
        };                                                                  // "Listener1" and "Listener2" are just for demonstration, not used in this example

        listener1.Start();  // <-------------add itself to static MeterListener's static `s_allStartedListeners`


        // Second MeterListener
        var listener2 = new MeterListener();
        
        listener2.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>
        {
            Console.WriteLine($"Listener2 received: {value}");
        });
        
        listener2.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "my_counter")
                listener.EnableMeasurementEvents(instrument, "Listener2");  // <-------add new ListenerSubscription(listener2, "Listener2") to instrument's _subscriptions
        };
        
        listener2.Start();  // <-------------add itself to static MeterListener's static `s_allStartedListeners`


        counter.Add(42);  // <-------------use _subscriptions to get listener1 and listener2, and call their NotifyMeasurement<T> method which call each listener's SetMeasurementEventCallback<T>
                          // method with the measurement value 42, optional tags and state ("Listener1" / "Listener2" in this example, normally it is MetricState )

        Console.ReadLine();   

        // Output:
        // Listener1 received: 42
        // Listener2 received: 42 
    }
}
//------------------------------------Ʌ

//------------------------------------V
class Program
{
    static void Main()
    {
        var meter = new Meter("DemoApp");
        var counter = meter.CreateCounter<int>("my_counter");

        var listener = new MeterListener();

        // First subscription with state "FirstState"
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "my_counter")
                meterListener.EnableMeasurementEvents(instrument, "FirstState");
        };
        listener.Start();

        // Now, subscribe again with a new state "SecondState"
        listener.EnableMeasurementEvents(counter, "SecondState"); // <---------- oldStateStored will be true here

        // The internal logic will:
        // - Remove the old subscription (with "FirstState")
        // - Add the new subscription (with "SecondState")
        // - Set oldStateStored = true and return the old state ("FirstState")
    }
}
//------------------------------------Ʌ

//------------------------------------V
/*
Suppose you want to log measurements to a file, and you need to rotate the log file at runtime (e.g., daily or when a file size limit is reached).
You can use the state object to hold the current file stream. When you rotate the log, you re-enable the listener with a new file stream as the state, 
and clean up the old one:
*/
class Program
{
    static Meter meter = new Meter("Example");
    static Counter<int> counter = meter.CreateCounter<int>("my_counter");

    static void Main()
    {
        const long maxLogSize = 1024; // 1 KB for demonstration
        string log1Path = "log1.txt";
        string log2Path = "log2.txt";
        StreamWriter currentLog = new StreamWriter(log1Path);
        string currentLogPath = log1Path;

        var listener = new MeterListener();
        listener.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>  // <--------------set MeterListener's _intMeasurementCallback so that this callback
        {                                                                              //  can be invoked when counter.Add() which triggers MeterListener's NotifyMeasurement
            var writer = state as StreamWriter;
            writer?.WriteLine($"Measurement: {value}");
            writer?.Flush();
        });

        listener.MeasurementsCompleted = (instrument, oldState) =>
        {
            var oldWriter = oldState as StreamWriter;
            if (oldWriter != null)
            {
                oldWriter.WriteLine("Closing log file.");
                oldWriter.Dispose();
            }
        };

        // initial subscription with log1.txt
        listener.EnableMeasurementEvents(counter, currentLog);

        // simulate measurements and log rotation
        for (int i = 0; i < 1000; i++)
        {
            counter.Add(i);   // <--------------------------------invoke the SetMeasurementEventCallback above

            currentLog.Flush();
            FileInfo fi = new FileInfo(currentLogPath);
            if (fi.Length >= maxLogSize && currentLogPath == log1Path)  // check if current log file is full
            {
                // Switch to log2.txt
                StreamWriter newLog = new StreamWriter(log2Path);
                string oldLogPath = currentLogPath;
                currentLogPath = log2Path;
                listener.EnableMeasurementEvents(counter, newLog); // <--------------------this will trigger MeasurementsCompleted for log1
                currentLog = newLog;
                Console.WriteLine($"Switched to new log file: {log2Path}");
            }

            Thread.Sleep(10); // Simulate time between measurements
        }

        Console.ReadLine();
    }
}
//------------------------------------Ʌ
```


===================================================================================================================================================


## How PeriodicExportingMetricReader Collects Metrics

An instance of `public sealed class Metric` is a **metric stream** which is like a spreadsheet for a specific metric. Imagine a sheet tracking "HTTP requests" where each row represents a unique combination of tags (like method=GET, status=200). The whole sheet, the collection of all these rows for that metric is the metric stream, and each entry in the sheet is a time series `MetricPoints` instance. 

A `Metric` instance stores its `MetricPoints` through its internal `AggregatorStore`.

Imagine a music library (**Instrument**) containing all songs. A **view** is like creating a playlist with specific criteria—such as "Top 10 Jazz Songs from the 1990s" or "All Songs by Artist X." Each playlist (view) selects, groups, and organizes songs in a particular way. The resulting playlist is the metric stream: it’s a concrete, filtered, and organized collection based on the rules of the view. Multiple playlists (views) can be created from the same library, each producing a different metric stream.


## AggregatorStore 

```C#
var meter = new Meter("MyApp");
var counter = meter.CreateCounter<int>("requests");

counter.Add(1, KeyValuePair.Create("region", "us-east"));
counter.Add(1, KeyValuePair.Create("region", "eu-west"));
counter.Add(1, KeyValuePair.Create("region", "us-east"));
```

```yml
AggregatorStore
  ├─ MetricPoint[0]: region=us-east, value=2
  └─ MetricPoint[1]: region=eu-west, value=1
  ```
