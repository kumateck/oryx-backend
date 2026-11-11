# Employee email and linked accounts

Use **Human Resource → Employee management → Update employee email** to change
an employee’s email and the email used by their linked account. The server
uses the saved employee-to-user link and saves both updates in one transaction.
The existing `CanUpdateEmployeeEmail` permission covers this action. A general
profile edit that changes the email of an employee with a linked account
requires both `CanUpdateEmployeeInfo` and `CanUpdateEmployeeEmail`.

When an account uses its email as its username, the username follows the new
email. The user signs in with the new email and their existing password.
Changing the account email invalidates previous password reset links. The
normal **Forgot password** flow sends a fresh link when needed.

To repair an existing mismatch after deploying the backend, open **Update
employee email** and confirm the correct address already shown on the employee
record. Then refresh the Users page and verify the linked account. If the
employee has no saved account link, confirm the account belongs to the employee
before correcting that link through the account administration process.
An address already in use is rejected before either record changes.
