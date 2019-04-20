# Blockchain integration block reader

This component of blockchain integration is intended for retrieving blocks and transactions data from blockchain.

To implement blocks reader component:

1. Consider implementing dedicated service to provide an abstraction of blockchain client API. Such approach helps to write well structured and testable component.

2. Implement `IBlockReader` interface for retrievng blocks and transactions data.

3. If blockchain supports blocks irreversibility then choose either `pull` or `push` model for retrievng last irreversible block data.

    - Use `push` model if used blockchain tools allow you to determine changes of last irreversible block number (f.e. via web-sockets). Use `IIrreversibleBlockListener` provided by DI to emit `LastIrreversibleBlockUpdatedEvent`.

    - Use `pull` model otherwise, implement `IIrreversibleBlockProvider` interface.

4. Provide implementations to SDK code in `ConfigureServices()` method on startup, register corresponding strategy for retrieving last irreversible block data, if necessary. Use `options.UseSettings` delegate to register additional services in DI container.

5. Provide integration name and other options in `Configure()` method on startup.

