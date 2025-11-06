// Copyright (C) Eventuous HQ OÜ. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Azure.ServiceBus.Shared;
using Eventuous.Subscriptions;

namespace Eventuous.Azure.ServiceBus.Subscriptions;

/// <summary>
/// Options for configuring a Service Bus subscription.
/// </summary>
public record ServiceBusSubscriptionOptions : SubscriptionOptions {
    /// <summary>
    /// Gets or sets the queue or topic to subscribe to.
    /// </summary>
    public required IQueueOrTopic QueueOrTopic { get; set; }

    /// <summary>
    /// Gets or sets the options for the Service Bus processor.
    /// </summary>
    public ServiceBusProcessorOptions ProcessorOptions { get; set; } = new();

    /// <summary>
    /// Session processor options (если работаем с RequiresSession).
    /// If not specified, it will be built from <see cref="ProcessorOptions"/>.
    /// </summary>
    public ServiceBusSessionProcessorOptions? SessionProcessorOptions { get; set; }

    /// <summary>
    /// An indication that an entity in Service Bus is created with RequiresSession = true and a session processor needs to be built.
    /// </summary>
    public bool RequiresSession { get; set; } = false;

    /// <summary>
    /// Gets the message attributes for Service Bus messages.
    /// </summary>
    public ServiceBusMessageAttributeNames AttributeNames { get; init; } = new();

    /// <summary>
    /// Gets the error handler delegate for processing errors.
    /// </summary>
    public Func<ProcessErrorEventArgs, Task>? ErrorHandler { get; init; }
}

/// <summary>
/// Represents a queue or topic for Service Bus subscriptions.
/// </summary>
public readonly record struct ServiceBusProcessorFactoryResult(
    ServiceBusProcessor? Processor,
    ServiceBusSessionProcessor? SessionProcessor)
{
    public static implicit operator ServiceBusProcessorFactoryResult(ServiceBusProcessor processor)
        => new(processor, null);

    public static implicit operator ServiceBusProcessorFactoryResult(ServiceBusSessionProcessor sessionProcessor)
        => new(null, sessionProcessor);
}

public interface IQueueOrTopic {
    /// <summary>
    /// Creates a <see cref="ServiceBusProcessor"/> for the specified client and options.
    /// </summary>
    /// <param name="client">The Service Bus client.</param>
    /// <param name="options">The subscription options.</param>
    /// <returns>A configured <see cref="ServiceBusProcessor"/> instance.</returns>
    ServiceBusProcessorFactoryResult MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options);
}

/// <summary>
/// Represents a Service Bus queue.
/// </summary>
public record Queue(string Name) : IQueueOrTopic {
    /// <summary>
    /// Creates a <see cref="ServiceBusProcessor"/> for the queue.
    /// </summary>
    /// <param name="client">The Service Bus client.</param>
    /// <param name="options">The subscription options.</param>
    /// <returns>A configured <see cref="ServiceBusProcessor"/> for the queue.</returns>
    public ServiceBusProcessorFactoryResult MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options) {
        if (options.RequiresSession) {
            var sessionOptions = BuildSessionOptions(options);
            return client.CreateSessionProcessor(Name, sessionOptions);
        }

        return client.CreateProcessor(Name, options.ProcessorOptions);
    }

    static ServiceBusSessionProcessorOptions BuildSessionOptions(ServiceBusSubscriptionOptions options) {
        // We construct from standard options to avoid duplicating the configuration
        var sessionOptions = options.SessionProcessorOptions ?? new ServiceBusSessionProcessorOptions();
        sessionOptions.AutoCompleteMessages = options.ProcessorOptions.AutoCompleteMessages;
        sessionOptions.PrefetchCount = options.ProcessorOptions.PrefetchCount;
        sessionOptions.MaxAutoLockRenewalDuration = options.ProcessorOptions.MaxAutoLockRenewalDuration;
        sessionOptions.ReceiveMode = options.ProcessorOptions.ReceiveMode;
        sessionOptions.Identifier = options.ProcessorOptions.Identifier;
        return sessionOptions;
    }
}

/// <summary>
/// Represents a Service Bus topic.
/// </summary>
public record Topic(string Name) : IQueueOrTopic {
    /// <summary>
    /// Creates a <see cref="ServiceBusProcessor"/> for the topic and subscription ID from options.
    /// </summary>
    /// <param name="client">The Service Bus client.</param>
    /// <param name="options">The subscription options.</param>
    /// <returns>A configured <see cref="ServiceBusProcessor"/> for the topic.</returns>
    public ServiceBusProcessorFactoryResult MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options) {
        if (options.RequiresSession) {
            var sessionOptions = BuildSessionOptions(options);
            return client.CreateSessionProcessor(Name, options.SubscriptionId, sessionOptions);
        }

        return client.CreateProcessor(Name, options.SubscriptionId, options.ProcessorOptions);
    }

    static ServiceBusSessionProcessorOptions BuildSessionOptions(ServiceBusSubscriptionOptions options) {
        var sessionOptions = options.SessionProcessorOptions ?? new ServiceBusSessionProcessorOptions();
        sessionOptions.AutoCompleteMessages = options.ProcessorOptions.AutoCompleteMessages;
        sessionOptions.PrefetchCount = options.ProcessorOptions.PrefetchCount;
        sessionOptions.MaxAutoLockRenewalDuration = options.ProcessorOptions.MaxAutoLockRenewalDuration;
        sessionOptions.ReceiveMode = options.ProcessorOptions.ReceiveMode;
        sessionOptions.Identifier = options.ProcessorOptions.Identifier;
        return sessionOptions;
    }
}

/// <summary>
/// Represents a Service Bus topic and a specific subscription.
/// </summary>
public record TopicAndSubscription(string Name, string Subscription) : IQueueOrTopic {
    /// <summary>
    /// Creates a <see cref="ServiceBusProcessor"/> for the topic and specified subscription.
    /// </summary>
    /// <param name="client">The Service Bus client.</param>
    /// <param name="options">The subscription options.</param>
    /// <returns>A configured <see cref="ServiceBusProcessor"/> for the topic and subscription.</returns>
    public ServiceBusProcessorFactoryResult MakeProcessor(ServiceBusClient client, ServiceBusSubscriptionOptions options) {
        if (options.RequiresSession) {
            var sessionOptions = BuildSessionOptions(options);
            return client.CreateSessionProcessor(Name, Subscription, sessionOptions);
        }

        return client.CreateProcessor(Name, Subscription, options.ProcessorOptions);
    }

    static ServiceBusSessionProcessorOptions BuildSessionOptions(ServiceBusSubscriptionOptions options) {
        var sessionOptions = options.SessionProcessorOptions ?? new ServiceBusSessionProcessorOptions();
        sessionOptions.AutoCompleteMessages = options.ProcessorOptions.AutoCompleteMessages;
        sessionOptions.PrefetchCount = options.ProcessorOptions.PrefetchCount;
        sessionOptions.MaxAutoLockRenewalDuration = options.ProcessorOptions.MaxAutoLockRenewalDuration;
        sessionOptions.ReceiveMode = options.ProcessorOptions.ReceiveMode;
        sessionOptions.Identifier = options.ProcessorOptions.Identifier;
        return sessionOptions;
    }
}
