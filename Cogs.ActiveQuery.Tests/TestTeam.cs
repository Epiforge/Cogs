using Cogs.Collections.Synchronized;
using Cogs.Components;
using System;

namespace Cogs.ActiveQuery.Tests
{
    class TestTeam : PropertyChangeNotifier, IComparable<TestTeam>
    {
        public TestTeam() : this(new SynchronizedRangeObservableCollection<TestPerson>())
        {
        }

        public TestTeam(SynchronizedRangeObservableCollection<TestPerson>? people) => this.people = people;

        SynchronizedRangeObservableCollection<TestPerson>? people;

        public int CompareTo(TestTeam? other) => GetHashCode().CompareTo(other?.GetHashCode() ?? 0);

        public SynchronizedRangeObservableCollection<TestPerson>? People
        {
            get => people;
            set => SetBackedProperty(ref people, in value);
        }
    }
}
