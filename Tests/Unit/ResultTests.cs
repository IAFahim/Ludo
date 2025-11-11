using NUnit.Framework;
using Ludo;
using System;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for Result&lt;T, TError&gt; type - Railway Oriented Programming
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("Result")]
    [Category("ROP")]
    public class ResultTests
    {
        [Test]
        public void Ok_CreatesSuccessResult()
        {
            var result = Result<int, string>.Ok(42);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.IsErr, Is.False);
            Assert.That(result.Unwrap(), Is.EqualTo(42));
        }

        [Test]
        public void Err_CreatesErrorResult()
        {
            var result = Result<int, string>.Err("error");

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.IsOk, Is.False);
            Assert.That(result.UnwrapErr(), Is.EqualTo("error"));
        }

        [Test]
        public void Unwrap_OnErr_ThrowsException()
        {
            var result = Result<int, string>.Err("error");

            Assert.Throws<InvalidOperationException>(() => result.Unwrap());
        }

        [Test]
        public void UnwrapErr_OnOk_ThrowsException()
        {
            var result = Result<int, string>.Ok(42);

            Assert.Throws<InvalidOperationException>(() => result.UnwrapErr());
        }



        [Test]
        public void Map_OnOk_TransformsValue()
        {
            var result = Result<int, string>.Ok(42);
            var mapped = result.Map(x => x * 2);

            Assert.That(mapped.IsOk, Is.True);
            Assert.That(mapped.Unwrap(), Is.EqualTo(84));
        }

        [Test]
        public void Map_OnErr_PropagatesError()
        {
            var result = Result<int, string>.Err("error");
            var mapped = result.Map(x => x * 2);

            Assert.That(mapped.IsErr, Is.True);
            Assert.That(mapped.UnwrapErr(), Is.EqualTo("error"));
        }

        [Test]
        public void AndThen_OnOk_ChainsResult()
        {
            var result = Result<int, string>.Ok(42);
            var chained = result.AndThen(x => Result<string, string>.Ok(x.ToString()));

            Assert.That(chained.IsOk, Is.True);
            Assert.That(chained.Unwrap(), Is.EqualTo("42"));
        }

        [Test]
        public void AndThen_OnErr_PropagatesError()
        {
            var result = Result<int, string>.Err("error");
            var chained = result.AndThen(x => Result<string, string>.Ok(x.ToString()));

            Assert.That(chained.IsErr, Is.True);
            Assert.That(chained.UnwrapErr(), Is.EqualTo("error"));
        }

        [Test]
        public void MapErr_OnErr_TransformsError()
        {
            var result = Result<int, string>.Err("error");
            var mapped = result.MapErr(e => e.Length);

            Assert.That(mapped.IsErr, Is.True);
            Assert.That(mapped.UnwrapErr(), Is.EqualTo(5));
        }

        [Test]
        public void MapErr_OnOk_PreservesValue()
        {
            var result = Result<int, string>.Ok(42);
            var mapped = result.MapErr(e => e.Length);

            Assert.That(mapped.IsOk, Is.True);
            Assert.That(mapped.Unwrap(), Is.EqualTo(42));
        }



        [Test]
        public void TryGetValue_OnOk_ReturnsTrue()
        {
            var result = Result<int, string>.Ok(42);
            bool success = result.TryGetValue(out int value, out string error);

            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void TryGetValue_OnErr_ReturnsFalse()
        {
            var result = Result<int, string>.Err("error");
            bool success = result.TryGetValue(out int value, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("error"));
        }

        [Test]
        public void Ensure_WithTrue_ReturnsOk()
        {
            var result = ResultExtensions.Ensure(true, "error");
            Assert.That(result.IsOk, Is.True);
        }

        [Test]
        public void Ensure_WithFalse_ReturnsErr()
        {
            var result = ResultExtensions.Ensure(false, "error");
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo("error"));
        }

        [Test]
        public void Tap_OnOk_ExecutesAction()
        {
            int sideEffect = 0;
            var result = Result<int, string>.Ok(42);
            result.Tap(x => sideEffect = x);

            Assert.That(sideEffect, Is.EqualTo(42));
        }

        [Test]
        public void Tap_OnErr_DoesNotExecuteAction()
        {
            int sideEffect = 0;
            var result = Result<int, string>.Err("error");
            result.Tap(x => sideEffect = x);

            Assert.That(sideEffect, Is.EqualTo(0));
        }
    }
}
