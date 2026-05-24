

$(document).ready(function () {
	$('.btn-add-cart').click(function () {
		var Id = $(this).data('product_id');

		$.ajax({
			type: "POST",
			url: "@Url.Action("AddToCart", "Cart")",
			data: { Id: Id }, // Dữ liệu được gửi đến server

			success: function (result) {
				if (result) {
					Swal.fire({
						toast: true,
						position: 'top-end',
						icon: 'success',
						title: 'Đã thêm vào giỏ hàng',
						showConfirmButton: false,
						timer: 1500
					});
				}
			},
		});
	});
});


$(document).ready(function () {

    $('.add-wishlist-icon').click(function () {

        var button = $(this);
        var Id = button.data('product_id');

        $.ajax({
            type: "POST",
            url: "@Url.Action("AddToWishlist", "Home")",
            data: { Id: Id },

            success: function (result) {

                if (result.success) {

                    Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: result.added ? 'success' : 'info',
                        title: result.message,
                        showConfirmButton: false,
                        timer: 1500
                    });

                    button.toggleClass('active', result.added);
                }

            }
        });

    });

});