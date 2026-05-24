namespace ShopDongHo.Models
{
    public class Paginate
    {
        public int TotalItems { get; private set; }// tổng số lượng data trong db
        public int PageSize { get; private set; } // tổng item/trang
        public int CurrentPage { get; private set; } 
        public int TotalPages { get; private set; } // tổng số trang
        public int StartPage { get; private set; } 
        public int EndPage { get; private set; }
        
        public Paginate()
        {

        }
        public Paginate(int totalItems, int page, int pageSize = 10)
        {
            //làm tròn số trang : 33/10 = 3.3 => 4 trang
            int totalPages = (int)Math.Ceiling(totalItems / (decimal)pageSize);
            int currentPage = page; // trang hiện tại =1
            int startPage =currentPage - 5; // trang bắt đầu trừ 5 button (chưa hỉu lắm)
            int endPage = currentPage + 4; // trang cuối + thêm 4 button

            if(startPage < 0)
            {
                // nếu trang bắt đầu <= 0 thì số trang cuối sẽ bangef 
                endPage = endPage - (startPage - 1);
                startPage = 1;
            }
            if(endPage > totalPages)// nếu số page cuôi > số tổng trang
            {
                endPage = totalPages;
                if(endPage > 10)
                {
                    startPage = endPage - 9;
                }
            }

            TotalItems = totalItems;
            CurrentPage = currentPage;
            TotalPages = totalPages;
            StartPage = startPage;
            EndPage = endPage;
        }
    }
}
