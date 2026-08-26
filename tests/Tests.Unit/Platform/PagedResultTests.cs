using NexaEcommerce.SharedKernel.Pagination;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Platform;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(45, 20, 3)]
    public void TotalPages_is_the_ceiling_of_items_over_page_size(
        int total,
        int pageSize,
        int expected)
    {
        var result = PagedResult<string>.Create(
            [],
            page: 1,
            pageSize: pageSize,
            totalItems: total);

        result.TotalPages.ShouldBe(expected);
    }

    [Fact]
    public void Middle_page_has_both_neighbours()
    {
        var result = PagedResult<string>.Create(
            [],
            page: 2,
            pageSize: 10,
            totalItems: 35);

        result.HasPrev.ShouldBeTrue();
        result.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void First_page_has_no_previous()
    {
        var result = PagedResult<string>.Create(
            [],
            page: 1,
            pageSize: 10,
            totalItems: 35);

        result.HasPrev.ShouldBeFalse();
        result.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void Last_page_has_no_next()
    {
        var result = PagedResult<string>.Create(
            [],
            page: 4,
            pageSize: 10,
            totalItems: 35);

        result.HasPrev.ShouldBeTrue();
        result.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void Items_and_paging_inputs_are_carried_through()
    {
        IReadOnlyList<string> items = ["a", "b"];

        var result = PagedResult<string>.Create(
            items,
            page: 1,
            pageSize: 20,
            totalItems: 2);

        result.Items.ShouldBe(items);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
        result.TotalItems.ShouldBe(2);
    }
}