# DelayBug
### Task.Delay and Thread.Sleep behave differently if Visual Studio is running

I recently started working on a project which requires a task/thread to run as fast as possible, without hogging the CPU.

To accomplish this I used a `Thread.Sleep` statement with a delay of 2 milliseconds.

Everything worked just fine until I ran the program directly (outside of VS).
Sometimes the program worked fine, others it presented some serious lag and stuttering.

After many hours of testing different things I realized that the program would only work correctly when Visual Studio was running! Even with an empty solution, as long as VS was opened, the program worked correctly.

So I created a small and simple application that can be used to verify this behavior (for delays of less than 15ms).

Here's a summarized output of the tests performed with the sample program, which clearly illustrate the problem (under Windows).

                      |                        Windows                        |          Linux            |
                      |-------------------------------------------------------|---------------------------|
                      |          VS OFF           |          VS ON            |    mono    |    dotnet    |
                      |---------------------------|---------------------------|------------|--------------|
                      |  .NET 4.8  |  NetCore 3.1 |  .NET 4.8  |  NetCore 3.1 |  .NET 4.8  |  NetCore 3.1 |
    ------------------|------------|--------------|------------|--------------|------------|--------------|
    Thread.Sleep(2)   |   8.3473   |    8.3512    |   1.5853   |    1.2383    |   1.0722   |    1.0897    |
    ------------------|------------|--------------|------------|--------------|------------|--------------|
    Task.Delay(2)     |   8.3712   |    8.3617    |   7.8118   |    7.8130    |   1.0544   |    2.0021    |
    ------------------|------------|--------------|------------|--------------|------------|--------------|
    AutoResetEvent    |            |              |            |              |            |              |
    WaitOne(2)        |   8.2800   |    8.2729    |   1.2828   |    1.2442    |     n/t    |      n/t     |
    ------------------|------------|--------------|------------|--------------|------------|--------------|
    AutoResetEvent    |            |              |            |              |            |              |
    WaitOne()         |   8.3531   |    8.3669    |   7.8113   |    7.8136    |     n/t    |      n/t     |
    Triggered by Timer|            |              |            |              |            |              |
    -------------------------------------------------------------------------------------------------------

The code in the Test app tries to calculate the exact value of a millisecond using this code:

    Task.Run(() => {
        long curTick = DateTime.Now.Ticks;
        long lastTick = curTick;
    
        for(int i = 0; i < iters; i++) {
            delayMethod.Invoke();
    
            curTick = DateTime.Now.Ticks;
            accTicks += (curTick - lastTick);
            lastTick = curTick;
        }
    }).Wait();

    return accTicks * mspt / (delay * iters);

Where `mstp` is the number of milliseconds per tick, `delay` is the delay in milliseconds (in this example is set to 2) and `iters` is the number of times the code is executed.

*Ideally*, every single result should have returned `"1.0000"`.

Seeing that, for example, under Windows without running VS the delay took up to 8 times more than it should have seems to indicate a serious issue with the Delay/Sleep implementation.

So I guess my questions are:

1) Why does this happen?
2) Why does it only affect Windows and not Linux?
3) How can one "reliably" work with small delays (<15ms) without having the rely on native calls (winmm.dll)?
