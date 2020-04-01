namespace RetailBusinessLogic
{
    using System;

    public class FashionItem
    {
        public int Size { get; set; }
        public string Description { get; set; }
        public FashionType FashionType { get; set; }
        public decimal Price { get; set; }
    }

    public class FashionQuery
    {
        public int Size { get; set; }
        public FashionType FashionType { get; set; }
    }

    public class BusinessData
    {
        public Func<FashionItem, decimal> Markup { get; set; }
    }

    public class ProcessingContext
    {
        public FashionQuery Query { get; set; }
        public BusinessData BusinessData { get; set; }
    }

    public class FashionType
    {
        public string Value { get; set; }

        public static FashionType From(string value) => new FashionType(value);

        private FashionType(string value)
        {
            this.Value = value;
        }

        public override bool Equals(object obj) => (obj is FashionType) && string.Equals(((FashionType)obj).Value, this.Value);
        public override int GetHashCode() => this.Value.GetHashCode();
        public override string ToString() => this.Value;

        public static readonly FashionType TShirt = FashionType.From("T-Shirt");
        public static readonly FashionType Pullover = FashionType.From("Pullover");
        public static readonly FashionType Shoes = FashionType.From("Shoes");
        public static readonly FashionType Hat = FashionType.From("Hat");
        public static readonly FashionType Trousers = FashionType.From("Trousers");
    }
}