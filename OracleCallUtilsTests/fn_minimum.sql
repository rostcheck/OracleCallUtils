-- PL/SQL function for testing
create or replace function minimum 
(
  v1 in number 
, v2 in number 
) return number as 
begin
  if (v1 > v2) then
    return v2;
  else
    return v1;
  end if;
end minimum;