using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MarukoLib.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarukoLib.Tests
{

    [TestClass]
    public class ContextTests
    {

        public class NamedProperty<T> : ContextProperty<T>
        {

            public NamedProperty(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

            [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
            public string Name { get; }

            public override string ToString() => Name;

        }

        private static readonly ContextProperty<string> Name = new NamedProperty<string>("Name");

        private static readonly ContextProperty<uint> Age = new NamedProperty<uint>("Age");

        private static readonly ContextProperty<string> Address = new NamedProperty<string>("Address");

        [TestMethod]
        public void TestTransactionalContext()
        {
            var context = new TransactionalContext();
            var transactionManager = context.CreateTransactionManager();
            Name.Set(transactionManager, "Lonely Cat");
            Age.Set(transactionManager, 16);
            Debug.WriteLine(context.ToDictionary());
            transactionManager.Commit();
            transactionManager.Delete(Name);
            Age.Set(transactionManager, 14);
            Address.Set(transactionManager, "Hangzhou");
            Debug.WriteLine(context.ToDictionary());
            transactionManager.Rollback();
            Debug.WriteLine(context.ToDictionary());
        }

    }

}
