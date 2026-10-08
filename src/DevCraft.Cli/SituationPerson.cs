namespace DevCraft.Cli;

public sealed record SituationPerson(
    string RowId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Relation,
    string Status);
