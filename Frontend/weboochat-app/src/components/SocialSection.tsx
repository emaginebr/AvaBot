import { Button } from "@/components/ui/button";
import { Twitter, Instagram, Linkedin, Music } from "lucide-react";

const SocialSection = () => {
  const socialLinks = [
    {
      name: "Twitter",
      icon: Twitter,
      url: "https://twitter.com/webooChat",
      color: "hover:text-[#1DA1F2]",
      bg: "hover:bg-[#1DA1F2]/10"
    },
    {
      name: "Instagram",
      icon: Instagram,
      url: "https://instagram.com/webooChat",
      color: "hover:text-[#E4405F]",
      bg: "hover:bg-[#E4405F]/10"
    },
    {
      name: "LinkedIn",
      icon: Linkedin,
      url: "https://linkedin.com/company/webooChat",
      color: "hover:text-[#0077B5]",
      bg: "hover:bg-[#0077B5]/10"
    },
    {
      name: "TikTok",
      icon: Music,
      url: "https://tiktok.com/@webooChat",
      color: "hover:text-[#FF0050]",
      bg: "hover:bg-[#FF0050]/10"
    }
  ];

  return (
    <section className="py-16 bg-muted/20">
      <div className="container mx-auto px-4 lg:px-8">
        <div className="text-center mb-12">
          <h2 className="text-3xl md:text-4xl font-bold text-foreground mb-4">
            Follow us on
            <span className="bg-gradient-primary bg-clip-text text-transparent"> Social Media</span>
          </h2>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
            Stay updated with news, tips and updates from WebooChat
          </p>
        </div>

        <div className="flex justify-center space-x-6">
          {socialLinks.map((social, index) => (
            <Button
              key={index}
              variant="glass"
              size="lg"
              className={`group transition-all duration-300 ${social.bg} ${social.color}`}
              onClick={() => window.open(social.url, '_blank')}
            >
              <social.icon className="w-6 h-6 transition-transform group-hover:scale-110" />
              <span className="hidden sm:inline ml-2">{social.name}</span>
            </Button>
          ))}
        </div>

        {/* Newsletter */}
        <div className="mt-12 text-center">
          <div className="max-w-md mx-auto">
            <h3 className="text-xl font-semibold text-foreground mb-3">
              Exclusive Newsletter
            </h3>
            <p className="text-muted-foreground mb-4">
              Get weekly tips on customer service automation
            </p>
            <div className="flex space-x-2">
              <input
                type="email"
                placeholder="Your best email"
                className="flex-1 px-4 py-2 rounded-lg bg-card border border-primary/20 text-foreground placeholder-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
              />
              <Button variant="hero" size="sm">
                Subscribe
              </Button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default SocialSection;