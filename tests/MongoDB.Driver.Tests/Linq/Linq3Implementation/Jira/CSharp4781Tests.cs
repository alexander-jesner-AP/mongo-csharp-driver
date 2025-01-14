﻿/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4781Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Enumerable_DefaultIfEmpty_should_return_default_int()
        {
            var c = new C { Id = 1, IntArray = new int[0] };

            var result = c.IntArray.DefaultIfEmpty();

            result.Should().Equal(new int[] { 0 });
        }

        [Fact]
        public void Enumerable_DefaultIfEmpty_should_return_default_string()
        {
            var c = new C { Id = 1, StringArray = new string[0] };

            var result = c.StringArray.DefaultIfEmpty();

            result.Should().Equal(new string[] { null });
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_DefaultIfEmpty_should_return_default_int(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.IntArray.DefaultIfEmpty());

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $let : { vars : { source : '$IntArray' }, in : { $cond : { if : { $eq : [{ $size : '$$source' }, 0] }, then : [0], else : '$$source' } } } }, _id : 0 } }");

                var results = queryable.ToArray();
                results.Should().HaveCount(2);
                results[0].Should().Equal(new int[] { 1 });
                results[1].Should().Equal(new int[] { 0 });
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_DefaultIfEmpty_should_return_default_string(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.StringArray.DefaultIfEmpty());

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $let : { vars : { source : '$StringArray' }, in : { $cond : { if : { $eq : [{ $size : '$$source' }, 0] }, then : [null], else : '$$source' } } } }, _id : 0 } }");

                var results = queryable.ToArray();
                results.Should().HaveCount(2);
                results[0].Should().Equal(new string[] { "abc" });
                results[1].Should().Equal(new string[] { null });
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, IntArray = new int[] { 1 }, StringArray = new string[] { "abc" } },
                new C { Id = 2, IntArray = new int[] { }, StringArray = new string[] { } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] IntArray { get; set; }
            public string[] StringArray { get; set; } // note that string is suitable for this test since it does not have a default constructor
        }
    }
}
