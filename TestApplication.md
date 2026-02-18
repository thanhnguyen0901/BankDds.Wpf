HOW TO TEST THE APPLICATION
Test Users (already configured):

admin / 123 → NganHang (ALL branches)
btuser / 123 → ChiNhanh (BENTHANH only)
tduser / 123 → ChiNhanh (TANDINH only)
c123456 / 123 → KhachHang (BENTHANH, CMND=c123456)
Test Scenarios:

Login as admin → Verify all tabs visible and full CRUD works
Test Employee Management: Add, Edit, Delete, Restore, Transfer Branch
Test Account Management: Select customer, add/edit/delete accounts
Test Transactions: Deposit 200,000 VND, Withdraw 100,000 VND, Transfer between accounts
Test Reports: Generate account statement, accounts opened report, customers per branch
Test Admin: Add new users with different roles
Login as btuser → Verify branch restriction (only BENTHANH data)
Login as c123456 → Verify customer can only see Reports tab and their own accounts