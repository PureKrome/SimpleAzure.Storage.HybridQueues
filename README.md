<h1 align="center">Simple: Azure Storage Hybrid Queues</h1>

<div align="center">
  Making it simple to add your items to an Azure Storage Queue and Blob (if required).<br/><br/>
  <i>Stop worrying if your the item will be too big for the queue.</i>
</div>

<br />

<div align="center">
    <!-- License -->
    <a href="https://choosealicense.com/licenses/mit/">
    <img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License - MIT" />
    </a>
    <!-- NuGet -->
    <a href="https://www.nuget.org/packages/WorldDomination.SimpleAzure.Storage.HybridQueues/">
    <img src="https://buildstats.info/nuget/WorldDomination.SimpleAzure.Storage.HybridQueues" alt="NuGet" />
    </a>
</div>


---
## Overview

Queues are a common computer-science concept: **a system that stores an ordered, linear sequence of Items**.
Usually the Items are `Messages` which is just fancy-pants container your data + other special metadata.

Messages have a size limit, though. For example, [Azure Storage Queues have a size limit of 64KB for Plain Text messages or 48KB for Base64 Encoded](https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction).

So if you try and place your content into a queue and the content is too big, then you will get an error.

<h4 align="center">Enter ➡️ Hybrid Queue's.</h4>
<br/>

A **Hybrid Queue** is the concept of throwing _anything_ onto a normal Queue and if the size of the Message
(which contains your content) is too big, it then _automatically_ puts your content into a Blob (which
can contain _any size_**) and then stores the _reference to the blob item_ in the queue!

Both directions (sending a message to the queue and popping a message off the queue) handle the smarts if the message is
too big and needs to retrieve the contents from the blob.

Under the hood, if the content is not a [Primitive Type](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types)
like a `string` or `int`, etc (more or less), then we convert the content to a Json representation of the source item.
So if you have a custom POCO, it's serialized to Json, then stored in the queue or blob, based on the final size.

<h4 align="center">So now handling larger content with Queues is made Simple!</h4>

_Note about message size limits: the Azure Queue SDK doesn't allow you to ask what Encoding is used for messages for the queue.
Base64? Plain Text? As such, we need to assume the worst and set the max size limit to the lower-sized Base64 limit of 48KB.
Anything larger will then be stored in a blob._

---

## Installation

[![](https://i.imgur.com/oLtAwq9.png)](https://www.nuget.org/packages/WorldDomination.SimpleAzure.Storage.HybridQueues/)

Package Name: `WorldDomination.SimpleAzure.Storage.HybridQueues`  
CLI: `install-package WorldDomination.SimpleAzure.Storage.HybridQueues`  

## 💻 TL;DR; Show me some code!

### 1. Some simple content (e.g. a string or a number)

```c#

// _queueClient, _blobContainerClient and _logger would be injected via your IoC/DI
// These are normally setup in elsewhere, like in your program.cs, etc.
// e.g.
//    _queueClient = new QueueClient(connectionStringText, "test-queue");
//    _blobContainerClient = new BlobContainerClient(connectionStringText, "test-container");

// Create the Hybrid Queue.
var hybridQueue = new HybridQueue(_queueClient, _blobContainerClient, logger);

// Content to store on a queue.
var message = "hello";

// Adding the content to the queue.
await hybridQueue.AddMessageAsync(message, cancellationToken);

// The queue message will contain the value 'hello'. Nothing will be placed into the blob container.
```

### 2. A simple POCO

```
// Create our POCO.
public record User(string Name, int Age);
var user = new User("Pure Krome", 100);

// Create the Hybrid Queue.
var hybridQueue = new HybridQueue(_queueClient, _blobContainerClient, logger);

// Adding the content to the queue.
await hybridQueue.AddMessageAsync(user, cancellationToken);

// The queue message will contain the value '{"name":"Pure Krome", "age": 100}'

```

### 3. A 'Large' POCO that will not fit into a Queue.

```
// Generate some really long content larger than the queue size.
const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

var length = queueClient.MessageMaxBytes + 1; // Just larger than the queue message max size.
var longName = new string("a", length);

// Create our POCO
public record User(string Name, int Age);
var user = new User(longName, 100);

// Create the Hybrid Queue.
var hybridQueue = new HybridQueue(_queueClient, _blobContainerClient, logger);

// Adding the content to the queue.
await hybridQueue.AddMessageAsync(user, cancellationToken);


// The queue message will contain the value <Some Guid>'
// The blob container will contain a blob with the Json representation of that POCO.
// The Blob will have the name <Some Guid>, so the Queue Message "links" to this Blob.
```

## Contract / Methods available for you to use

- `AddMessageAsync` : Single item.
- `AddMessagesAsync` : multiple items added at once. Batching if the collection of items is large.
- `GetMessageAsync` : Single item.
- `GetMessagesAsync` : Multiple messages.
- `DeleteMessageAsync` : Single message. Knows if it needs to remove it from queue and blob (if required).

---

## Contribute
Yep - contributions are always welcome. Please read the contribution guidelines first.

## Code of Conduct

If you wish to participate in this repository then you need to abide by the code of conduct.

## Feedback

Yes! Please use the Issues section to provide feedback - either good or needs improvement :cool:

---
