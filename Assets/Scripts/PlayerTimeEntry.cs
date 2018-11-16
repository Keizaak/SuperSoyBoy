using System;

[Serializable] //allows the binary formatter to (de)serialize (= (un)pack) data class 
public class PlayerTimeEntry{

    public DateTime entryDate;
    public decimal time;
}
