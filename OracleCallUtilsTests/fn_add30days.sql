-- PL/SQL function for tests
create or replace function add30days 
(
  in_date in date 
) return date as 
begin
  return in_date + 30;
end add30days;