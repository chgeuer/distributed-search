# distributed-search

Small demo for running a distributed search. 

## Setup

To run this demo, copy `Credentials/creds.json` into your home directory, and insert appropriate settings... 

This will change in the future, once the app components completely run in Docker and on Kubernetes.

## Business Logic

### Filters

Let's say we have the following domain data types:

```fsharp
type FashionQuery = 
    { Size: int
      FashionType: FashionType }

type FashionItem =
    { Size: int
      FashionType: FashionType
      Price: decimal 
      Description: string
      StockKeepingUnitID : string }
```

We want to create a filter predicate which only let's `FashionItem`s pass which have the right size. This filter can be implemented like this:

#### From scratch in C#

```csharp
public class SizeFilter : IBusinessLogicFilterPredicate<ProcessingContext, FashionItem>
{
    bool IBusinessLogicFilterPredicate<ProcessingContext, FashionItem>.Matches(ProcessingContext context, FashionItem item)
        => context.Query.Size == item.Size;
}
```

#### Using a generic class in C#

```csharp
new GenericFilter<ProcessingContext, FashionItem>((c,i) => c.Query.Size == i.Size)
```

#### From scratch in F# 

```fsharp
type SizeFilter() =
    interface IBusinessLogicFilterPredicate<ProcessingContext, FashionItem> with
        member this.Matches(ctx, item) = ctx.Query.Size = item.Size
```

## Note to self

- Discriminated unions get properly serialized in .NET, so I probably don't need this one https://gist.github.com/isaacabraham/ba679f285bfd15d2f53e
