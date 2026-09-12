using Xunit;

// Live integration tests boot a real dedicated server on a fixed port and write to a shared savedir,
// so only one may run at a time. Serialize the whole assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
