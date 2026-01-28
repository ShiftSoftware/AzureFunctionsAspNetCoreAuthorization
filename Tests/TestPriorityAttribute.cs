using Xunit.Sdk;
using Xunit.v3;
using System.Reflection;

namespace Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; private set; }

    public TestPriorityAttribute(int priority) => Priority = priority;
}

public class PriorityOrderer : ITestCaseOrderer
{
    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases)
        where TTestCase : notnull, ITestCase
    {
        var sortedMethods = new SortedDictionary<int, List<TTestCase>>();

        foreach (TTestCase testCase in testCases)
        {
            int priority = 0;
            
            // Get assembly, type and method info from the test case
            var testClassName = testCase.TestClassName;
            var testMethodName = testCase.TestMethodName;
            
            // Find the assembly containing the test
            var testAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetType(testClassName) != null);
            
            if (testAssembly != null)
            {
                var type = testAssembly.GetType(testClassName);
                if (type != null)
                {
                    var methodInfo = type.GetMethod(testMethodName);
                    if (methodInfo != null)
                    {
                        var priorityAttribute = methodInfo.GetCustomAttribute<TestPriorityAttribute>();
                        if (priorityAttribute != null)
                        {
                            priority = priorityAttribute.Priority;
                        }
                    }
                }
            }

            GetOrCreate(sortedMethods, priority).Add(testCase);
        }

        var orderedCases = sortedMethods.Keys
            .SelectMany(priority => sortedMethods[priority]
                .OrderBy(testCase => testCase.TestMethodName))
            .ToList();

        return orderedCases;
    }

    private static TValue GetOrCreate<TKey, TValue>(
        IDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : struct
        where TValue : new() =>
        dictionary.TryGetValue(key, out TValue? result)
            ? result
            : (dictionary[key] = new TValue());
}