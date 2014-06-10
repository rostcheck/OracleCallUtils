-- Oracle PL/SQL procedure called by test functions

create or replace PROCEDURE P_EMP_RS 
(
  p_department_id IN employees.department_id%TYPE 
, p_recordset OUT SYS_REFCURSOR 
) AS 
BEGIN
  OPEN p_recordset FOR
    SELECT first_name,
           last_name, 
           employee_id,
           department_id
    FROM   employees
    WHERE  department_id = p_department_id
    ORDER BY last_name;
END P_EMP_RS;