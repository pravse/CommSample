//
// 
// Partial class to augment 
// 
namespace WcfCommService {

    /// The class is generated automatically by running xsd.exe against poll.xsd.
    /// Here, we extend the partial class with some specific extra methods
    public partial class PollType 
    {
	// default constructor (for serialization)
	public PollType()
	{
	}

	// constructor with all the required fields
	public PollType(PollTypeType thisType, bool isHidden, string thisTitle, string thisLevels, string thisDescription)
	{
	    SetType(thisType);
	    SetHidden(isHidden);
	    title = thisTitle;
	    levels = thisLevels;
	    description = thisDescription;
	}

	// helper methods to set properties that have the "specified" extra fields (wierdness of XMLSerialization)
	public void SetType(PollTypeType thisType)
	{
	    this.type = thisType;
	    this.typeSpecified = true;
	}

	public void SetHidden(bool isHidden)
	{
	    this.hidden = isHidden;
	    this.hiddenSpecified = true;
	}

	public void SetState(StatesType thisState)
	{
	    this.state = thisState;
	    this.stateSpecified = true;
	}

    }
}
